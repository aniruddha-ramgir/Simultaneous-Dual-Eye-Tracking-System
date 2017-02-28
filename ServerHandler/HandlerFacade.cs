using Microsoft.Win32;
using System;
using System.IO;
using System.Messaging;
using System.Threading;

namespace ServerHandler
{
    static class HandlerFacade
    {
        //private static readonly object MsgBox;
        public static HandlerObserver Observer;
        public static Int32 _port { get; private set; }
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main() //Entry point
        {
            string[] args = Environment.GetCommandLineArgs(); //Get port number as as the only argument.
            _port = Convert.ToInt32(args[1]);
            String _ConfigPath = "C:\\config\\"+ _port.ToString()+".cfg"; //path can be stored in a editable resource. OR within this project, use relative path
            String _ServerPath = GetServerExecutablePath();
            //Start ServerHandler
            if (paraprocess.Program.launch(_port, _ConfigPath, _ServerPath) != true)
            {
                return;
            }
            //Instantiate MSMQ Observer object
            //System.Windows.Forms.MessageBox.Show("Hi?");
            Observer = new HandlerObserver(_port);

            //Start UI only if the ServerHandler has begun
            ServerHandler.App app = new ServerHandler.App();
            app.InitializeComponent();
            app.Run();
            System.Windows.Forms.MessageBox.Show("Hi");
            //Code below will be executed only when Calibrator is exited.
            #region MSMQing
            //if (paraprocess.Program.Alpha.IsActivated() == true)
            //while(true)
            // {
            Observer.Begin();
                //Observer.listenIncomingQueue();
           // }
            #endregion
        }

        private static string GetServerExecutablePath()
        {
            // check default paths           
            const string x86 = "C:\\Program Files (x86)\\EyeTribe\\Server\\EyeTribe.exe";
            if (File.Exists(x86))
                return x86;

            const string x64 = "C:\\Program Files\\EyeTribe\\Server\\EyeTribe.exe";
            if (File.Exists(x64))
                return x64;

            // Still not found, let user select file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".exe";
            dlg.Title = "Please select the Eye Tribe server executable";
            dlg.Filter = "Executable Files (*.exe)|*.exe";

            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            return string.Empty;
        }


    }
    class HandlerObserver
    {
        Thread ObserverThread;
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;
        public string IncomingQueueName { get; private set; }
        public string OutgoingQueueName { get; private set; }
        Message Response;
        bool RequestReceived = false;
        Int32 port;
        Message receivedMessage = null;
        public HandlerObserver(Int32 _port) //creates a queue using port for the Handler process.
        {
            //ObserverThread = new Thread(this.Run);
            port = _port;
            Run();
            //ObserverThread.Start();
        }
        void Run()
        {
            Response = new Message();
            receivedMessage = new Message();
            try
            {
                IncomingQueueName = port.ToString() + "RQ";
                OutgoingQueueName = port.ToString() + "RE";
                if (MessageQueue.Exists(@".\Private$\" + IncomingQueueName))
                {
                    //Just assign them to our Request queue objects, if they already exist.
                    IncomingQueue = new MessageQueue(@".\Private$\" + IncomingQueueName);
                }
                else
                {
                    // Create the Request Queue
                    MessageQueue.Create(@".\Private$\" + IncomingQueueName);
                    IncomingQueue = new MessageQueue(@".\Private$\" + IncomingQueueName);

                }

                if (MessageQueue.Exists(@".\Private$\" + OutgoingQueueName))
                {
                    //Just assign them to our Respone queue objects, if they already exist.
                    OutgoingQueue = new MessageQueue(@".\Private$\" + OutgoingQueueName);
                }
                else
                {
                    // Create the Response Queue
                    MessageQueue.Create(@".\Private$\" + OutgoingQueueName);
                    OutgoingQueue = new MessageQueue(@".\Private$\" + OutgoingQueueName);

                }

                IncomingQueue.MulticastAddress = "234.1.1.1:8001"; //can be passed an command line argument
                System.Windows.Forms.MessageBox.Show("multicast set");
                
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK13", e.Source);
            }
        }
        public void Begin()
        {
            while (true)
            {
                this.listenIncomingQueue();
                SpinWait.SpinUntil(() => RequestReceived); //This will block the thread

                System.Windows.Forms.MessageBox.Show(RequestReceived.ToString());
            }
        }
        public bool executeMessage(Message msg)
        {
            string label = null;
            string body = null;
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                label = msg.Label.ToString();

                body = msg.Body.ToString();
                System.Windows.Forms.MessageBox.Show(body + "CHEK11" + label);
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error getting label and body-" + e);
            }
            switch (label) //act based on the label Type
            {
                case "ACK":
                    {
                        System.Windows.Forms.MessageBox.Show(body + label);
                        return handleAcknowledgement(body);
                    } 
                case "NOTIF":
                    {
                        System.Windows.Forms.MessageBox.Show(body + label);
                        return handleNotification(body);
                    }
                case "REQ":
                    {
                        System.Windows.Forms.MessageBox.Show(body + label);
                        return handleRequest(body);
                    } 
                case "EXCEPTION":
                    {
                        System.Windows.Forms.MessageBox.Show(body + label);
                        return handleException(body);
                    }
                case "ERR":
                    {
                        System.Windows.Forms.MessageBox.Show(body + label);
                        return handleException(body); //In Future, handle errors in a better way
                    }
                default: return false;
            }
        }
        public bool handleAcknowledgement(string body)
        {
            return true;
        }
        public bool handleException(string body)
        {
            if (System.Windows.Forms.MessageBox.Show(body) == System.Windows.Forms.DialogResult.OK)
            {
                return true;
            }
            return false;
        }
        public bool handleNotification(string body)
        {
            return true;
        }
        public bool handleRequest(string body) //return true if request handled
        {
            switch (body)
            {
                case "ready":
                    {
                        if (paraprocess.Program.Alpha.IsCalibrated() == true)
                        {
                            System.Windows.Forms.MessageBox.Show("Fake calibrated.");
                            sendMessage("ready","ACK");
                            return true;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Please calibrate the Trackers."); //retry if OK, else, ERR. Or PsychoPy can send a message
                            sendMessage("ready", "ERR");
                            return true;
                        }
                    }
                case "record":
                    {
                        if (paraprocess.Program.Alpha.IsCalibrated() == true)
                        {
                            paraprocess.Program.Alpha.StartListening();
                            if (paraprocess.Program.Alpha.IsListening() == true)
                            {
                                sendMessage("record","ACK");
                                return true;
                            }
                            else
                            {
                                sendMessage("record", "ERR");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "pause":
                    {
                        sendMessage("pause","ACK");
                        return paraprocess.Program.Alpha.StopListening();
                    }
                case "stop":
                    {
                        sendMessage("stop","ACK");
                        return paraprocess.Program.Alpha.Deactivate();
                    }
                default: return false;
            }
        }
        public void listenIncomingQueue() //receives Message and attaches CorrelationID to the "response" message object
        {
            try
            {
                IncomingQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(MyReceiveCompleted);

                // Begin the asynchronous receive operation.
                IncomingQueue.BeginReceive();
            }
            catch(Exception e)
            {
              //  m.Body = e.Message;
              //  m.Label = "EXCEPTION";
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK13");
            }
        }
        private void MyReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {
            //Message receivedMessage = new Message();
            RequestReceived = false;
            MessageQueue workingQueue = null;
            try
            {
                // Connect to the queue.
                workingQueue = (MessageQueue)source;

                // End the asynchronous receive operation.
                receivedMessage = workingQueue.EndReceive(asyncResult.AsyncResult);

                // Process Message
                Response = receivedMessage;
                Response.CorrelationId = receivedMessage.Id; //sets the ID of the received message as CorrID to Reply (RESPONSE) message.
                //executeMessage(receivedMessage);
                if (executeMessage(receivedMessage) == true)
                    RequestReceived = true;
                else
                    RequestReceived = false;

                // Restart the asynchronous receive operation.
                //workingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                //Response.Body = "EXCEPTION";
                //Response.Label = "EXCEPTION";
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK12");
            }
        }
      /*  public void sendMessage(Message msg) //attaches Response Queue and sends (redundant?)
        {
            msg.ResponseQueue = IncomingQueue;
            msg.CorrelationId = Response.CorrelationId;
            OutgoingQueue.Send(msg);
        } */
        /*public void sendMessage(string msgString) //create message object for a string and sends it. Attaches appropriate label to it.
        {
            Response.Body = msgString;
            Response.Label = setLabel(msgString);
            Response.ResponseQueue = IncomingQueue;
            OutgoingQueue.Send(Response);
        } */
        public void sendMessage(string msgString,string label) //create message object for a string and sends it.
        {
            Response = new Message();
            Response.Body = msgString;
            Response.Label = label;
            Response.ResponseQueue = IncomingQueue;
            System.Windows.Forms.MessageBox.Show("CHEK-Pre9"+msgString+label+OutgoingQueue.Path);
            OutgoingQueue.Send(Response);
            System.Windows.Forms.MessageBox.Show("CHEK9");
        }
       /* private string setLabel(string body) //attached appropriate label
        {
            switch (body)
            {
                case "calibrated":
                    {
                        return "NOTIF";
                    }
                 default:
                    {
                        return "ACK";
                    }
            }
        } */
    }

}
