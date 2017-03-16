using Microsoft.Win32;
using ServerHandler.Properties;
using System;
using System.IO;
using System.Messaging;
using System.Threading;

namespace ServerHandler
{
    static class HandlerFacade //Entry point for the ServerHandler Process
    {
        #region relevant objects and variables
       public static string logFilePathName = Resources.logPath+string.Format(@"{0}.txt", DateTime.Now.Ticks);
       public static Thread ObserverWorker = null;
       public static HandlerObserver Observer;
       public static Int32 _port { get; private set; }
        #endregion

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

            //If SDET log folder doesn't exist, the below line creates it.
            System.IO.Directory.CreateDirectory(Resources.logPath);

            File.AppendAllText(logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "This is the log file for the Tracker associated with the port number:" + _port.ToString()+ Environment.NewLine);
            File.AppendAllText(logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Inside HandlerFacade Main()" + Environment.NewLine);
            
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
        //public Thread ObserverWorker = null;
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
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Inside HandlerObserver Constructor." + Environment.NewLine);
            port = _port;
            receivedMessage = new Message();
            try
            {
                IncomingQueueName = _port.ToString() + "RQ";
                OutgoingQueueName = _port.ToString() + "RE";

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
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @HandlerObserver Constructor: " + e + Environment.NewLine);
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
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "HandlerObserver has begun to receive messages." + Environment.NewLine);
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @listenIncomingQueue: " + e + Environment.NewLine);
            }
        } 
        private void MyReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {
            MessageQueue workingQueue = null;
            try
            {
                #region set up receiving Queue
                workingQueue = new MessageQueue();
                workingQueue = (MessageQueue)source;
                workingQueue.MessageReadPropertyFilter.Body = true;
                workingQueue.MessageReadPropertyFilter.Label = true;
                #endregion

                // End the asynchronous receive operation.
                receivedMessage = workingQueue.EndReceive(asyncResult.AsyncResult);

                // Process Message and log event
                if (executeMessage(receivedMessage))
                    File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "HandlerObserver has received a message and it was executed." + Environment.NewLine);
                else
                    File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "HandlerObserver has received a message and it was not executed." + Environment.NewLine);
               
                //Restart the asynchronous receive operation.
                workingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @MyReceiveCompleted: " + e + Environment.NewLine);
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
            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @executeMessage: " + e + Environment.NewLine);
            }
            switch (label) //act based on the label Type
            {
                case "REQ":
                    {
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
                        return handleException(body);
                    }
                case "ERR":
                    {
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
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "How did you get here?. Stimuli-Module should not send ACKs to the Handler." + body+Environment.NewLine);
            return true;
        }
        public bool handleException(string body)
        {
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception was received from the FactoryFacade ->This should not be happening, because FactoryFacade should not send any ERRs or EXCEPTIONSs to ServerHandler.---" + body + Environment.NewLine);
            /* if (System.Windows.Forms.MessageBox.Show(body) == System.Windows.Forms.DialogResult.OK)
             {
             //No interruption is necessary as we are logging everything.
                 return true;
             }
             return false; */
            return true;
        }
        public bool handleNotification(string body)
        {
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "How did you get here?. Stimuli-Module should not send NOTIFs to the Handler." + body + Environment.NewLine);
            return true;
        }
        #endregion

        public bool handleRequest(string body) //return true if request handled
        {
            switch (body)
            {
                case "ready":
                    {
                        if (!paraprocess.Program.Alpha.IsCalibrated())
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Tracker is not calibrated. Sending ERR message back to the Factory" + Environment.NewLine);
                            return true;
                        }
                        if (paraprocess.Program.Alpha.preListening())
                        {
                            sendResponse(body, "ACK");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Tracker is calibrated. Response File was created. Sending acknowledgement back to the Factory." + Environment.NewLine);
                            return true;
                        }
                        return false;
                    }
                case "record":
                    {
                       /* if (!paraprocess.Program.Alpha.IsCalibrated())
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, port.ToString() + " Tracker is not calibrated. Sending ERR. Calibrate before recording." + Environment.NewLine);
                            return false;
                        } */
                        paraprocess.Program.Alpha.StartListening();
                        if (paraprocess.Program.Alpha.IsListening())
                        {
                            sendResponse(body, "ACK");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") +"|"+ port.ToString() + " Tracker is listening and is receiving gazeData now." + Environment.NewLine);
                            return true;
                        }
                        else
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "|"+port.ToString() + " Tracker is not listening and is not receiving gazeData now." + Environment.NewLine);
                            return false;
                        }
                    }
                case "pause":
                    {
                        if (paraprocess.Program.Alpha.pauseListening())
                        {
                            sendResponse(body, "ACK");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Pause success." + Environment.NewLine);
                            return true;
                        }
                        else
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Pause failed.." + Environment.NewLine);
                            return true;
                        }
                    }
                case "resume":
                    {
                        paraprocess.Program.Alpha.resumeListening();
                        if (paraprocess.Program.Alpha.IsListening())
                        {
                            sendResponse(body, "ACK");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Resume success." + Environment.NewLine);
                            return true;
                        }
                        else
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Resume failed.." + Environment.NewLine);
                            return true;
                        }
                    }
                case "stop":
                    {
                        if (!paraprocess.Program.Alpha.StopListening())
                        {
                            sendResponse(body, "ERR");
                            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Stop failed." + Environment.NewLine);
                            return false;
                        }
                        sendResponse(body,"ACK");
                        File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Stop success.Deactivating." + Environment.NewLine);
                        return paraprocess.Program.Alpha.Deactivate();
                    }

                default:
                    {
                        sendResponse(body, "UNKNOWN");
                        File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " Unrecognized body----" + body + Environment.NewLine);
                        return false;
                    }
            }
        }
        bool handleType(string body) //whether experiment type is test or real
        {
            try
            {
                if (body.ToLower() == "test")
                {
                    paraprocess.Program.Alpha.isTest = true;
                    sendResponse(body, "ACK");
                }
                else
                {
                    paraprocess.Program.Alpha.isTest = false;
                    sendResponse(body, "ACK");
                }
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Experiment type to " + body + "  was set succesfully." + Environment.NewLine);
                return true;
            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception occured at HandlerFacade.HandlerObserver.HandleType() : " +e + Environment.NewLine);
                return false;
            }
        }
        bool handleName(string body)
        {
            try
            {
                paraprocess.Program.Alpha._sessionName = body.ToLower();
                sendResponse(body, "ACK");
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Experiment session-name has been set to "+body+" succesfully." + Environment.NewLine);
                return true;
            }
            catch(Exception e)
            {
                sendResponse(body, "ERR");
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception occured at HandlerFacade.HandlerObserver.HandleName() : " + e + Environment.NewLine);
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
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Sending message to Factory Facade" + Environment.NewLine);
        }
    }
}
