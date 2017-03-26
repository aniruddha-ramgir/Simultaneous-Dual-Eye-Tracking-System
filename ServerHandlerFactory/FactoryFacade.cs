using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Messaging;
using System.Threading.Tasks;
using System.Globalization;
using System.Configuration;

namespace ServerHandlerFactory
{
    class FactoryFacade
    {
        Thread FacadeThread;
        public FactoryObserver Observer = null;

        #region relevant variables
        public static string logFilePathName = ConfigurationManager.AppSettings["logPath"] + string.Format(CultureInfo.InvariantCulture,@"{0}.txt", DateTime.Now.Ticks);
        int  Handler1ProcessID,Handler2ProcessID;
        public bool FactoryStarted { get; private set; }
        #endregion

        public FactoryFacade()
        {
            //If SDET log folder doesn't exist, the below line creates it.
            Directory.CreateDirectory(ConfigurationManager.AppSettings["logPath"]);
            //System.Windows.Forms.MessageBox.Show(ConfigurationManager.AppSettings["logPath"] + ConfigurationManager.AppSettings["HandlerPath"] + ConfigurationManager.AppSettings["incomingQueueName"] + ConfigurationManager.AppSettings["outgoingQueueName"]);
            File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Inside FactoryFacade Constructor. Creating new Thread for FactoryObserver." + Environment.NewLine);

            //This is to avoid UI from blocking. 
            FacadeThread = new Thread(this.Run);
            FacadeThread.Start();
        }

        void Run()
        {
            //use "for" loop and the below logic to make it work it multiple trackers
            if (IsHandlerProcessRunning() != true) //Extend this to work with more than 2 Handlers. 
                Handler1ProcessID= StartHandlerProcess("6555");

            //No need to wait here because, Handler waits internally after starting the server.
            Handler2ProcessID =  StartHandlerProcess("6556");

            //Process Affinity
            Process.GetProcessById(Handler1ProcessID).ProcessorAffinity = (IntPtr)1;
            Process.GetProcessById(Handler2ProcessID).ProcessorAffinity = (IntPtr)1;

            Observer = new FactoryObserver("6555", "6556");
            Observer.SyncRun();
        }

        #region Starting and exiting Handler process code
        private int StartHandlerProcess(string port)
        { 
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Normal;  
            psi.FileName = GetHandlerExecutablePath();
            psi.Arguments = port;
            Process processServer = null;
           
            if (string.IsNullOrEmpty(psi.FileName) || File.Exists(psi.FileName) == false)
            {
                FactoryStarted = false;
                return 0;
            }
            try
            {
                processServer = new Process();
                processServer.StartInfo = psi;
                processServer.Start();
                FactoryStarted = true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "An Exception has occured @StartHandlerProcess: " + e + Environment.NewLine);
                FactoryStarted = false;
            }
            Thread.Sleep(1000); // wait for it to spin up
            return processServer.Id;
        }

        private bool IsHandlerProcessRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLowerInvariant() == "ServerHandler")
                        return true;
                }
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "An Exception has occured @IsHandlerProcessRunning: " + e + Environment.NewLine);
                return false;
            }
            return false;
        }

        private string GetHandlerExecutablePath()
        {
            // ServerHandler default path from resource string           
            string def = ConfigurationManager.AppSettings["HandlerPath"];
            if (File.Exists(def))
                return def;

            // Still not found, let user select file
             OpenFileDialog dlg = new OpenFileDialog();
             dlg.DefaultExt = ".exe";
             dlg.Title = "Please select the EyeTribe Server Handler executable";
             dlg.Filter = "Executable Files (*.exe)|*.exe";

             if (dlg.ShowDialog() == true)
                 return dlg.FileName; 

            return string.Empty;
        }

        public void ExitServerHandlers() //THIS IS JUST A TEMPORARY fix. Make Handlers close themselves.
        {
            Process.GetProcessById(Handler1ProcessID).Kill();
            Process.GetProcessById(Handler2ProcessID).Kill();
        }
        #endregion
    }

    class FactoryObserver
    {
        #region Declaration statements for Message and MessageQueue Object
        //SDET Queue
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;

        //MULITCAST Queue. Handlers will receive requests from this queue.
        //MessageQueue multiRequestQueue = null;

        //Request Queues. Write messages to RQ.

        MessageQueue Handler1RQ = null;
        MessageQueue Handler2RQ = null;

        //Response Queues. Read messages from RE.
        MessageQueue Handler1RE = null;
        MessageQueue Handler2RE = null;
        
        //Message that is received from PsychoPy
        Message receivedMessage = null;

        //Message to be published to the  Handler queues
        //Message fwd = null;

        //Message to be sent back to PsychoPy
        Message Response = null;

        #endregion

        public FactoryObserver(string port1,string port2)
        {
            File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Inside FactoryObserver Constructor.Creating/assigning message Queues for FactoryObserver." + Environment.NewLine);
            try
            {
                //SET ACCESS MODES. 
                #region Incoming Queue
                if (MessageQueue.Exists(@".\Private$\" + ConfigurationManager.AppSettings["incomingQueueName"]))
                {
                    IncomingQueue = new MessageQueue(@".\Private$\" + ConfigurationManager.AppSettings["incomingQueueName"]);
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + ConfigurationManager.AppSettings["incomingQueueName"]);
                    IncomingQueue = new MessageQueue(@".\Private$\" + ConfigurationManager.AppSettings["incomingQueueName"]);
                }
                #endregion

                #region Outgoing Queue
                if (MessageQueue.Exists(@".\Private$\" + ConfigurationManager.AppSettings["outgoingQueueName"]))
                {
                    OutgoingQueue = new MessageQueue(@".\Private$\" + ConfigurationManager.AppSettings["outgoingQueueName"]);
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + ConfigurationManager.AppSettings["outgoingQueueName"]);
                    OutgoingQueue = new MessageQueue(@".\Private$\" + ConfigurationManager.AppSettings["outgoingQueueName"]);
                }
                #endregion

                //We actually do not have to initialize Request/Incoming queues for Handler processes.
                //Because we will be sending to the MULTI-CAST address.

                #region DOES NOT WORK- Multicast Queue
                // multiRequestQueue = new MessageQueue("FORMATNAME:MULTICAST=234.1.1.1:8001");
                #endregion

                //But as multicast is not working for some reason we are maintainaing RQ instances
                //Processes can create their own RQ and RE queues.
                //We are initialising then queues here JUST TO BE SAFE.

                #region Process1 Request Queue
                if (MessageQueue.Exists(@".\Private$\" + port1 + "RQ"))
                {
                    Handler1RQ = new MessageQueue(@".\Private$\" + port1 + "RQ");
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + port1 + "RQ");
                    Handler1RQ = new MessageQueue(@".\Private$\" + port1 + "RQ");
                }
                #endregion

                #region Process2 Request Queue
                if (MessageQueue.Exists(@".\Private$\" + port2 + "RQ"))
                {
                    Handler2RQ = new MessageQueue(@".\Private$\" + port2 + "RQ");
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + port2 + "RQ");
                    Handler1RQ = new MessageQueue(@".\Private$\" + port2 + "RQ");
                }
                #endregion

                #region Process1 Response Queue
                if (MessageQueue.Exists(@".\Private$\" + port1 + "RE"))
                {
                    Handler1RE = new MessageQueue(@".\Private$\" + port1 + "RE");
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + port1 + "RE");
                    Handler1RE = new MessageQueue(@".\Private$\" + port1 + "RE");
                }
                #endregion

                #region Process2 Response Queue
                if (MessageQueue.Exists(@".\Private$\" + port2 + "RE"))
                {
                    Handler2RE = new MessageQueue(@".\Private$\" + port2 + "RE");
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + port2 + "RE");
                    Handler2RE = new MessageQueue(@".\Private$\" + port2 + "RE");
                }
                #endregion

                #region Response Message object
                Response = new Message();
                #endregion
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "An Exception has occured @FactoryObserver Constructor: " + e + Environment.NewLine);
            }
        }

        public void SyncRun()
        {
            File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver SyncRun begins." + Environment.NewLine);

            #region set Queue Formatter
            IncomingQueue.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });

            Handler1RE.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
            Handler2RE.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
            #endregion

            #region set Queue PropertyFilters
            IncomingQueue.MessageReadPropertyFilter.SetDefaults();
            Handler1RE.MessageReadPropertyFilter.SetDefaults();
            Handler2RE.MessageReadPropertyFilter.SetDefaults();
            #endregion

            #region loop-regulating boolean variables
            bool runLoop = true;
           // bool Tracker1Calibrated = false;
           // bool Tracker2Calibrated = false;
            #endregion

            #region set Handler Queue PropertyFilters
            Handler1RE.MessageReadPropertyFilter.Body = true;
            Handler1RE.MessageReadPropertyFilter.Label = true;
            Handler2RE.MessageReadPropertyFilter.Body = true;
            Handler2RE.MessageReadPropertyFilter.Label = true;
            #endregion

            #region Loop that waits for "Calibrate" NOTIF-ication
            while (true)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Waiting for CalibrationRunner to send calibration results" + Environment.NewLine);

                #region Receiving from Handlers and setting message Formatters
                Message msg1 = null;
                Message msg2 = null;

                msg1 = Handler1RE.Receive();
                //msg1.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "A message from Handler1 has been received." + Environment.NewLine);
                msg2 = Handler2RE.Receive();
                // msg2.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "A message from Handler2 has been received." + Environment.NewLine);
                #endregion

                if (processHandlerReply(msg1) == "NOTIF" && processHandlerReply(msg2) == "NOTIF")
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Both trackers were calibrated. Proceeding." + Environment.NewLine);
                    break;
                }
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Unkown message received. Waiting for NOTIFs from both handlers." + Environment.NewLine);
            }
            #endregion

            #region Loop that sends Connect request to handlers.
            do
            {
                Message connectMessage = new Message("connect");
                connectMessage.Label = "REQ";
                Handler1RQ.Send(connectMessage);
                Handler2RQ.Send(connectMessage);

                bool Handler1ERR = false;
                bool Handler2ERR = false;
                bool notCorr = false;

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "connect-loop: Connecting..." + Environment.NewLine);

                #region Receiving from Handlers and setting message Formatters
                Message msg1 = Handler1RE.Receive();
                Message msg2 = Handler2RE.Receive();

                msg1.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                msg2.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                #endregion

                #region "BODY-based CORRELATION" if message from Handler1 is not correlated. 
                if (!msg1.Body.Equals(connectMessage.Body)) //Checks if message from Handler1 is correlated.
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Message received from Handler 1 does not correlate with the message received from the Stimuli-Module. Body= " + msg1.Body.ToString() + Environment.NewLine);
                    Handler1ERR = true;
                    notCorr = true;
                }
                #endregion
                #region "BODY-based CORRELATION" if message from Handler2 is not correlated.
                if (!msg2.Body.Equals(connectMessage.Body))
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Message received from Handler 2 does not correlate with the message received from the Stimuli-Module Body= " + msg2.Body.ToString() + Environment.NewLine);
                    Handler2ERR = true;
                    notCorr = true;
                }
                #endregion

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Messages received from both Handler 1 & 2 correlate with the message received from the Stimuli-Module" + Environment.NewLine);

                #region Handler1 has not received Acknowledgement
                if (processHandlerReply(msg1) != "ACK") //Deals with Handler1's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg1) == "ERR")
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'ERR' from Handler1" + Environment.NewLine);
                        Handler1ERR = true;
                    }
                    else //necessary?
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'UNKNOWN' from Handler1" + Environment.NewLine);
                        Handler1ERR = true;
                    }
                }
                #endregion
                #region Handler2 has not received Acknowledgement
                if (processHandlerReply(msg2) != "ACK") //Deals with Handler2's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg2) == "ERR")
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'ERR' from Handler2" + Environment.NewLine);
                        Handler2ERR = true;
                    }
                    else //is this necessary?
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'UNKNOWN' from Handler2" + Environment.NewLine);
                        Handler2ERR = true;
                    }
                }
                #endregion

                #region Handle unexpected issues here.
                if (Handler1ERR || Handler2ERR)
                {
                    if (notCorr)
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff",CultureInfo.InvariantCulture) + "Check Handler logs to identify correlation issues." + Environment.NewLine);
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Sending a ERR-message. Check Handler logs." + Environment.NewLine);
                    continue;
                }
                #endregion
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver connect-Loop ending." + Environment.NewLine);
                break;
            } while (true);

            #endregion

            #region Receiving-forwarding loop      
            do
            {
                bool Handler1ERR = false;
                bool Handler2ERR = false;
                bool notCorr = false;

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "At the beginning of the FactoryObserver SyncRun loop." + Environment.NewLine);
                receivedMessage = new Message();
                Response = new Message();

                //gets Message from Stimuli-Module
                receivedMessage = IncomingQueue.Receive();
                receivedMessage.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });

                //We cannot send one object to two different tasks at the same. Hence, a copy.
                Message receivedMessage_Copy = new Message();
                receivedMessage_Copy.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                receivedMessage_Copy.Body = receivedMessage.Body.ToString();
                receivedMessage_Copy.Label = receivedMessage.Label.ToString();

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver SyncRun Loop has received a message from Stimuli-module." + Environment.NewLine);

                //Parallel Sending. Using Task here does not seem smart, but its just to be on the safe side.
                //Task.Run(() => Handler1RQ.Send(receivedMessage));
                // Task.Run(() =>
                //Handler2RQ.Send(receivedMessage_Copy);

                //Task.Run does not work for some reason, so going for a "sequential Send".
                //Handler1RQ.Send(receivedMessage);
                //Handler2RQ.Send(receivedMessage);

                //Using Parallel.Invoke to send message to two different queues at the same time.
                //This is the best shot we have at parallel sending. 
                Parallel.Invoke(() => { Handler1RQ.Send(receivedMessage); }, () => { Handler2RQ.Send(receivedMessage_Copy); });

                /*  #region set Handler Queue PropertyFilters
                  Handler1RE.MessageReadPropertyFilter.Body = true;
                  Handler1RE.MessageReadPropertyFilter.Label = true;
                  Handler2RE.MessageReadPropertyFilter.Body = true;
                  Handler2RE.MessageReadPropertyFilter.Label = true;
                  #endregion */

                #region Receiving from Handlers and setting message Formatters
                Message msg1 = Handler1RE.Receive();
                Message msg2 = Handler2RE.Receive();

                msg1.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                msg2.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                #endregion

                #region "BODY-based CORRELATION" if message from Handler1 is not correlated. 
                if (!msg1.Body.Equals(receivedMessage.Body)) //Checks if message from Handler1 is correlated.
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Message received from Handler 1 does not correlate with the message received from the Stimuli-Module. Body= " + msg1.Body.ToString() + Environment.NewLine);
                    Handler1ERR = true;
                    notCorr = true;
                }
                #endregion
                #region "BODY-based CORRELATION" if message from Handler2 is not correlated.
                if (!msg2.Body.Equals(receivedMessage_Copy.Body))
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Message received from Handler 2 does not correlate with the message received from the Stimuli-Module Body= " + msg2.Body.ToString() + Environment.NewLine);
                    Handler2ERR = true;
                    notCorr = true;
                }
                #endregion

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Messages received from both Handler 1 & 2 correlate with the message received from the Stimuli-Module" + Environment.NewLine);

                #region Handler1 has not received Acknowledgement
                if (processHandlerReply(msg1) != "ACK") //Deals with Handler1's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg1) == "ERR")
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'ERR' from Handler1" + Environment.NewLine);
                        Handler1ERR = true;
                        //Response.Body = "Handler1";
                        //Response.Label = "ERR";
                    }
                    else //necessary?
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'UNKNOWN' from Handler1" + Environment.NewLine);
                        Handler1ERR = true;
                        //Response.Body = "Handler1";
                        //Response.Label = "UNKNOWN";
                    }
                    //Response.ResponseQueue = IncomingQueue;
                    //OutgoingQueue.Send(Response);
                }
                #endregion
                #region Handler2 has not received Acknowledgement
                if (processHandlerReply(msg2) != "ACK") //Deals with Handler2's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg2) == "ERR")
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'ERR' from Handler2" + Environment.NewLine);
                        Handler2ERR = true;
                        //Response.Body = "Handler2";
                        //Response.Label = "ERR";
                    }
                    else //is this necessary?
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received an 'UNKNOWN' from Handler2" + Environment.NewLine);
                        Handler2ERR = true;
                        //Response.Body = "Handler2";
                        //Response.Label = "UNKNOWN";
                    }
                    //Response.ResponseQueue = IncomingQueue;
                    //OutgoingQueue.Send(Response);
                    //continue;
                }
                #endregion

                #region Handle unexpected issues here.
                if (Handler1ERR || Handler2ERR)
                {
                    if (notCorr)
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Check Handler logs to identify correlation issues." + Environment.NewLine);
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Sending a ERR-message. Check Handler logs." + Environment.NewLine);
                    Response.Body = receivedMessage.Body;
                    Response.Label = "ERR";
                    Response.ResponseQueue = IncomingQueue;
                    OutgoingQueue.Send(Response);
                    continue;
                }
                #endregion

                if (receivedMessage.Body.ToString().ToLowerInvariant() == "stop")
                {
                    File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received a STOP-message. SyncRun will not loop from here on." + Environment.NewLine);
                    runLoop = false;
                }

                #region If all above are not satisfied -in order words- message is correlated and acknowledged
                Response.Body = receivedMessage.Body;
                Response.Label = "ACK";
                Response.ResponseQueue = IncomingQueue;
                OutgoingQueue.Send(Response);
                #endregion

                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver SyncRun Loop ending." + Environment.NewLine);
            } while (runLoop);
            #endregion
        }

        string processHandlerReply(Message msg)
        { 
            //This function sssumes that the calling method has checked for a correlation
            string label = null;
            string body = null;
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                label = msg.Label.ToString();
                body = msg.Body.ToString();
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "An Exception has occured @processHandlerReply: " + e + Environment.NewLine);
            }
            switch (label) //act based on the label Type
            {
                case "ACK": //successful events will fall in this category. This is the expected case.
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received: " + body + " - ACK - " + label + Environment.NewLine);
                        return "ACK";
                    }
                case "NOTIF": //only used for "calibrated" notification, which is sent by handler in the beginning.
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received: " + body + " - NOTIF - " + label + Environment.NewLine);
                        if (body == "calibrate")
                            return "NOTIF";
                        else
                            return "ERR";
                    }
                case "CALIB": //USELESS
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Received: " + body + " - CALIB - " + label + Environment.NewLine);
                        return "CALIB";
                    }
                case "EXCEPTION":
                    {
                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver has received an EXCEPTION from one of the Handlers: " + body + Environment.NewLine);
                        return "EXCEPTION";
                    }
                case "ERR":
                    {

                        File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "FactoryObserver has received an ERR from one of the Handlers: " + body + Environment.NewLine);
                        return "ERR";
                        //In Future, handle errors in a better way. Add --- retries; "Retrying message: count x" 
                    }
                default: return "UNKNOWN";
            }
        }

        /*string getBody(Message msg)
        {
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                string body = msg.Body.ToString().ToLower();
                return body;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandlerFactory.FactoryFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") +"An Exception has occured @getBody: " + e + Environment.NewLine);
                return e.Message;
            }

        } */

        /*  public void publish(String msg, String label)
          {
              Message m = new Message();
              m.Body = msg;
              m.Label = label;
              multiRequestQueue.Send(m);
          } */

    }
}
