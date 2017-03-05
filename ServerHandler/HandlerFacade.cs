using Microsoft.Win32;
using ServerHandler.Properties;
using System;
using System.IO;
using System.Messaging;
using System.Threading;

namespace ServerHandler
{
    static class HandlerFacade
    {
       public static Thread ObserverWorker = null;
       public static HandlerObserver Observer;
       public static Int32 _port { get; private set; }

        #region Do not touch
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        #endregion

        //Entry point
        public static void Main()
        {
            //Get port number as as the only argument.
            string[] args = Environment.GetCommandLineArgs(); 
            _port = Convert.ToInt32(args[1]);
            String _ConfigPath = Resources.configPath+_port.ToString()+".cfg"; //path can be stored in a editable resource. OR within this project, use relative path
            String _ServerPath = GetServerExecutablePath();

            //Start ServerHandler
            if (!paraprocess.Program.launch(_port, _ConfigPath, _ServerPath))
                return;

            #region MSMQing on a new Thread
            Observer = new HandlerObserver(_port);
            ObserverWorker = new Thread(Observer.listenIncomingQueue);
            ObserverWorker.Start();
            #endregion
            
            ServerHandler.App app = new ServerHandler.App();
            app.InitializeComponent();
            app.Run();
            
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
        public Thread ObserverWorker = null;
        #region Queue objects
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;
        #endregion

        #region Queue Names and port number
        public string IncomingQueueName { get; private set; }
        public string OutgoingQueueName { get; private set; }
        Int32 port;
        #endregion

        #region Message objects
        Message receivedMessage = null;
        Message Response = null;
        #endregion

        #region Constructor code
        public HandlerObserver(Int32 _port) //creates a queue using port for the Handler process.
        {
            port = _port;
            Run();
        }
        public void Run()
        {
            receivedMessage = new Message();
            try
            {
                IncomingQueueName = port.ToString() + "RQ";
                OutgoingQueueName = port.ToString() + "RE";
                #region IncomingQueue
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
                #endregion

                #region Outgoing Queue
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
                #endregion

                #region set Queue Formatter
                IncomingQueue.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                #endregion

                #region set Queue PropertyFilters
                IncomingQueue.MessageReadPropertyFilter.SetDefaults();
                #endregion

            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK13", e.Source);
            }
        }
        #endregion

        #region The methods that listen to the Message Queues
        public void listenIncomingQueue() //receives Message and sends back the "response" message object
        {
            try
            {
                IncomingQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(MyReceiveCompleted);

                // Begin the asynchronous receive operation.
                IncomingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK18");
            }
        } 
        private void MyReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {
            MessageQueue workingQueue = null;
            try
            {
                // Connect to the queue.
                workingQueue = new MessageQueue();

                workingQueue = (MessageQueue)source;
                //workingQueue.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                workingQueue.MessageReadPropertyFilter.Body = true;
                workingQueue.MessageReadPropertyFilter.Label = true;

                // End the asynchronous receive operation.
                receivedMessage = workingQueue.EndReceive(asyncResult.AsyncResult);

                //System.Windows.Forms.MessageBox.Show("Sent at: "+receivedMessage.SentTime.Millisecond.ToString()+"Arrived at:"+ receivedMessage.ArrivedTime.Millisecond.ToString());

                // Process Message
                executeMessage(receivedMessage);
                workingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK12");
            }
        }
        #endregion

        public bool executeMessage(Message msg)
        {
            string label = null;
            string body = null;
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
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
                case "REQ":
                    {
                        //System.Windows.Forms.MessageBox.Show("CHEK16" + body + label);
                        return handleRequest(body);
                    }
                case "TYPE":
                    {
                        return handleType(body);
                    }
                case "NAME":
                    {
                        return handleName(body);
                    }
                case "EXCEPTION":
                    {
                        //System.Windows.Forms.MessageBox.Show(body + label);
                        return handleException(body);
                    }
                case "ERR":
                    {
                        //System.Windows.Forms.MessageBox.Show(body + label);
                        return handleException(body); //In Future, handle errors in a better way
                    }
                case "NOTIF":
                    {
                        return handleNotification(body);
                    }
                case "ACK":
                    {
                        return handleAcknowledgement(body);
                    }
                default: return false;
            }
        }

        #region These methods execute "body" part of the message
        #region These are practically useless
        public bool handleAcknowledgement(string body)
        {
            System.Windows.MessageBox.Show("How did you get here?. Do not send ACKs to the Handler.");
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
            System.Windows.MessageBox.Show("How did you get here?. Do not send NOTIFs to the Handler.");
            return true;
        }
        #endregion

        public bool handleRequest(string body) //return true if request handled
        {
            switch (body)
            {
                case "ready":
                    {
                        if (paraprocess.Program.Alpha.IsCalibrated() == true)
                        {
                            //System.Windows.Forms.MessageBox.Show("Fake calibrated.");
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
                        if (!paraprocess.Program.Alpha.IsCalibrated())
                        {
                            System.Windows.Forms.MessageBox.Show(port.ToString()+"Not Calibrated.");
                            sendResponse(body, "ERR");
                            return false;
                        }
                        paraprocess.Program.Alpha.StartListening();
                        if (paraprocess.Program.Alpha.IsListening())
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
                        if (paraprocess.Program.Alpha.pauseListening())
                        {
                            System.Windows.Forms.MessageBox.Show("PAUSE failed. Sending ERR. CHEK23");
                            sendResponse(body, "ACK");
                            return true;
                        }
                        else
                        {
                            sendResponse(body, "ERR");
                            return true;
                        }
                    }
                case "stop":
                    {
                        if (!paraprocess.Program.Alpha.StopListening())
                        {
                            System.Windows.Forms.MessageBox.Show("STOP failed. Sending ERR. CHEK22");
                            sendResponse(body, "ERR");
                            return false;
                        }
                        sendResponse(body,"ACK");
                        return paraprocess.Program.Alpha.Deactivate();
                    }

                default:
                    {
                        sendResponse(body, "UNKNOWN");
                        return false;
                    }
            }
        }
        bool handleType(string body) //whether experiment type is test or real
        {
            try
            {
                if (body == "test")
                    paraprocess.Program.Alpha.isTest = true;
                else
                    paraprocess.Program.Alpha.isTest = false;
                return true;
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show("CHEK17" + e);
                return false;
            }
        }
        bool handleName(string body)
        {
            try
            {
                paraprocess.Program.Alpha._name = body;
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion 

        //create message object for a string and sends it.
        public void sendResponse(string body, string label) 
        {
            Response = new Message();
            Response.Body = body;
            Response.Label = label;
            OutgoingQueue.Send(Response);
            //System.Windows.Forms.MessageBox.Show("CHEK9");
        }
    }
}
