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
            Thread ObserverWorker = new Thread(Observer.listenIncomingQueue);
            ObserverWorker.Start();
            //Start UI only if the ServerHandler has begun
            ServerHandler.App app = new ServerHandler.App();
            app.InitializeComponent();
            app.Run();
           // System.Windows.Forms.MessageBox.Show("Hi");
            //Code below will be executed only when Calibrator is exited.
            #region MSMQing
            //if (paraprocess.Program.Alpha.IsActivated() == true)
            //while(true)
            // {
            //Observer.Begin();
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
       // Thread ObserverThread;
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;
        public string IncomingQueueName { get; private set; }
        public string OutgoingQueueName { get; private set; }
       // bool RequestReceived = false;
        Int32 port;
        Message Response;
        Message receivedMessage = null;
        public HandlerObserver(Int32 _port) //creates a queue using port for the Handler process.
        {
            //ObserverThread = new Thread(this.Run);
            port = _port;
            Run();
           // ObserverThread.Start();
        }
        public void Run()
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

                   // IncomingQueue.MulticastAddress = "234.1.1.1:8001"; //can be passed an command line argument
                    //System.Windows.Forms.MessageBox.Show("multicast set");
                }
                else
                {
                    // Create the Request Queue
                    MessageQueue.Create(@".\Private$\" + IncomingQueueName);
                    IncomingQueue = new MessageQueue(@".\Private$\" + IncomingQueueName);
                   // IncomingQueue.SetPermissions("ANONYMOUS LOGON", MessageQueueAccessRights.WriteMessage);

                    //IncomingQueue.MulticastAddress = "234.1.1.1:8001"; //can be passed an command line argument
                                                                       // IncomingQueue.SetPermissions("ANONYMOUS LOGON", MessageQueueAccessRights.ReceiveMessage);
                                                                       //System.Windows.Forms.MessageBox.Show("multicast set");

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
                
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK13", e.Source);
            }
        }
       /*( public void Begin()
        {
                this.listenIncomingQueue();
               // SpinWait.SpinUntil(() => RequestReceived); //This will block the thread
        } */
        public bool executeMessage(Message msg)
        {
            string label = null;
            string body = null;
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                Response.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                label = msg.Label.ToString();
                body = msg.Body.ToString();
               // System.Windows.Forms.MessageBox.Show("CorrID:"+Response.CorrelationId.ToString()+"ID:"+msg.Id.ToString()+"=="+body + "CHEK11" + label);
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
                        System.Windows.Forms.MessageBox.Show("CHEK16"+body + label);
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
                            sendResponse(body,"ACK");
                            return true;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Please calibrate the Trackers."); //retry if OK, else, ERR. Or PsychoPy can send a message
                            sendResponse(body, "ERR");
                            return true;
                        }
                    }
                case "record":
                    {
                        if (paraprocess.Program.Alpha.IsCalibrated() != true)
                        {
                            sendResponse(body, "ERR");
                            return false;
                        }
                        paraprocess.Program.Alpha.StartListening();
                        if (paraprocess.Program.Alpha.IsListening() == true)
                        {
                            sendResponse(body, "ACK");
                            return true;
                        }
                        else
                        {
                            sendResponse(body, "ERR");
                            return false;
                        }
                    }
                case "pause":
                    {
                        //add pause code here
                        sendResponse(body,"ACK");
                        return paraprocess.Program.Alpha.StopListening();
                    }
                case "stop":
                    {
                        //Stop code to be added here
                        sendResponse(body,"ACK");
                        return paraprocess.Program.Alpha.Deactivate();
                    }
                default: return false;
            }
        }
        public void listenIncomingQueue() //receives Message and sends back the "response" message object
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
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK17");
            }
        }
        private void MyReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {

            #region set Queue Formatter
            IncomingQueue.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
            #endregion

            #region set Queue PropertyFilters
            IncomingQueue.MessageReadPropertyFilter.SetDefaults();
            #endregion

            //RequestReceived = false;
            MessageQueue workingQueue = null;
            Response = new Message(); //reset Response whenever a new message is received
            try
            {
                // Connect to the queue.
                workingQueue = new MessageQueue();

                workingQueue = (MessageQueue)source;
                workingQueue.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                //workingQueue.MessageReadPropertyFilter.CorrelationId = true;
                workingQueue.MessageReadPropertyFilter.Body = true;
                workingQueue.MessageReadPropertyFilter.Label = true;
                workingQueue.MessageReadPropertyFilter.SenderId = true;

                // End the asynchronous receive operation.
                receivedMessage = workingQueue.EndReceive(asyncResult.AsyncResult);
                receivedMessage.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                Response.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });

                System.Windows.Forms.MessageBox.Show("Triggered - Handler Receive");

                // Process Message
                //Response = receivedMessage;
                //Response.CorrelationId = receivedMessage.SenderId.ToString(); //sets the corrID of the received message as CorrID to Reply (RESPONSE) message.
                //System.Windows.Forms.MessageBox.Show("Response Corr: "+Response.CorrelationId.ToString());
                //executeMessage(receivedMessage);
                if (executeMessage(receivedMessage) == true)
                System.Windows.Forms.MessageBox.Show("CHEK18 + TRUEE. ");
                else
                    System.Windows.Forms.MessageBox.Show("CHEK18 + FALSEE. ");

                // Restart the asynchronous receive operation.
                workingQueue.BeginReceive();
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
        public void sendResponse(string body, string label) //create message object for a string and sends it.
        {
            //Response = new Message();
            Response.Body = body;
            Response.Label = label;
            Response.ResponseQueue = IncomingQueue; //USELESS
            //System.Windows.Forms.MessageBox.Show("CHEK-Pre9"+msgString+label+OutgoingQueue.Path);
            OutgoingQueue.Send(Response);
            //System.Windows.Forms.MessageBox.Show("CHEK9");
        }
        public void sendMessage(string msgString, string label) //create message object for a string and sends it.
        {
            Message msg = new Message();
            msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
            msg.Body = msgString;
            msg.Label = label;
            msg.ResponseQueue = IncomingQueue;
            //System.Windows.Forms.MessageBox.Show("CHEK-Pre9"+msgString+label+OutgoingQueue.Path);
            OutgoingQueue.Send(msg);
            //System.Windows.Forms.MessageBox.Show("CHEK9");
        }
    }

}
