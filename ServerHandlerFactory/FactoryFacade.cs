using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Messaging;
using System.Threading.Tasks;
using ServerHandlerFactory.Properties;

namespace ServerHandlerFactory
{
    class FactoryFacade
    {
        Thread FacadeThread;
        int  Handler1ProcessID,Handler2ProcessID;
        public bool FactoryStarted { get; private set; }
        public FactoryObserver Observer = null;
        public FactoryFacade()
        {
            //This is to avoid UI from blocking. Unnecessary 
            FacadeThread = new Thread(this.Run);
            FacadeThread.Start();
            //Run();
        }
        void Run()
        {
            //use for loop and the below logic to make it work it multiple trackers
            if (IsHandlerProcessRunning() != true) //Extend this to work with more than 2 Handlers. 
                Handler1ProcessID= StartHandlerProcess("6555");
            //No need to wait here because, Handler waits internally after starting the server.
            Handler2ProcessID =  StartHandlerProcess("6556");
            Observer = new FactoryObserver("6555", "6556");
            Observer.SyncRun();
            //Observer.Purge();
        }
        private int StartHandlerProcess(string port)
        { 
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Minimized; //set it to hidden 
            psi.FileName = GetHandlerExecutablePath();
            psi.Arguments = port;  //start with device 1 if its already running. //???
            Process processServer = null;
            if (psi.FileName == string.Empty || File.Exists(psi.FileName) == false)
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
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK8");
                FactoryStarted = false;
            }
            Thread.Sleep(3000); // wait for it to spin up
            return processServer.Id;
        }
        private bool IsHandlerProcessRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "ServerHandler")
                        return true;
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK1");
                return false;
            }
            return false;
        }
        private string GetHandlerExecutablePath()
        {
            // ServerHandler default path from resource string           
            string def = Resources.HandlerPath;
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

        public void ExitHandlers() //THIS IS JUST A TEMPORARY fix. Make Handlers close themselves.
        {
            Process.GetProcessById(Handler1ProcessID).Kill();
            Process.GetProcessById(Handler2ProcessID).Kill();
        }
    }
    class FactoryObserver
    {
        #region Declaration statements for Message and MessageQueue Object
        //SDET Queue
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;

        //MULITCAST Queue. Handlers will receive requests from this queue.
        MessageQueue multiRequestQueue = null;

        //Request Queues. Write messages to RQ.
        MessageQueue Handler1RQ = null;
        MessageQueue Handler2RQ = null;

        //Response Queues. Read messages from RE.
        MessageQueue Handler1RE = null;
        MessageQueue Handler2RE = null;
        
        //Message that is received from PsychoPy
        Message receivedMessage = null;

        //Message to be published to the  Handler queues
        Message fwd = null;

        //Message to be sent back to PsychoPy
        Message Response = null;

        #endregion

       /* #region
        Thread ObserverWorker1,ObserverWorker2;
        #endregion

        #region Boolean Identifiers that tell us if we have received any message from their respective senders.
        bool Handler1Received = false;
        bool Handler2Received = false;
        bool IncomingReceived = false;
        #endregion */

        public FactoryObserver(string port1,string port2)
        {
            //string incomingQueueName = "SDET-RQ";
            //outgoingQueueName = "SDET-RE";
            try
            {
                //SET ACCESS MODES. 
                #region Incoming Queue
                if (MessageQueue.Exists(@".\Private$\" + Resources.incomingQueueName))
                {
                    IncomingQueue = new MessageQueue(@".\Private$\" + Resources.incomingQueueName);
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + Resources.incomingQueueName);
                    IncomingQueue = new MessageQueue(@".\Private$\" + Resources.incomingQueueName);
                }
                #endregion

                #region Outgoing Queue
                if (MessageQueue.Exists(@".\Private$\" + Resources.outgoingQueueName))
                {
                    OutgoingQueue = new MessageQueue(@".\Private$\" + Resources.outgoingQueueName);
                }
                else
                {
                    MessageQueue.Create(@".\Private$\" + Resources.outgoingQueueName);
                    OutgoingQueue = new MessageQueue(@".\Private$\" + Resources.outgoingQueueName);
                }
                #endregion

                #region DOES NOT WORK- Multicast Queue
               // multiRequestQueue = new MessageQueue("FORMATNAME:MULTICAST=234.1.1.1:8001");
                #endregion  

                //We do not have to initialize Request/Incoming queues for Handler processes.
                //Because we will be sending to the MULTI-CAST address.
                //Processes can create their own RQ and RE queues.
                //We are initialising RE queues here JUST TO BE SAFE.
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
                    Handler1RE = new MessageQueue(@".\Private$\" + port2 + "RE");
                }
                #endregion

                #region Message objects
                Response = new Message();
                fwd = new Message();
                #endregion
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message+"CHEK1");
            }
        }
        public void SyncRun()
        {
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

            bool loop = true;
            //SDET starts with a "calibrate" NOTIFICATION from the Handlers.
            //Doesnt matter if I receive messages from both parallely or serially. 
            //I pick "SERIALLY" for now because I have to wait for one Handler or the other either way.

            if (processHandlerReply(Handler1RE.Receive()) =="NOTIF" && processHandlerReply(Handler2RE.Receive()) == "NOTIF")
            {
                //set "Ready" variable to true, MAYBE.
                System.Windows.Forms.MessageBox.Show("success1.");
            }
            //Start the loop of listening and forwarding.
            while (loop)
            {
                receivedMessage = new Message();
                fwd = new Message();
                Response = new Message();
                System.Windows.Forms.MessageBox.Show("Entered1.");

                //gets Message from PsychoPy
                receivedMessage  = IncomingQueue.Receive();
                receivedMessage.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                //fwd.ResponseQueue = IncomingQueue; //This statement is WRONG because, Handlers will reply to their own Response Queues.

                //Parallel Sending. Using Task here does not seem smart, but its just to be on the safe side.
                 //Task.Run(() => Handler1RQ.Send(fwd));
                // Task.Run(() => Handler2RQ.Send(fwd));
                Handler1RQ.Send(receivedMessage);
                Handler2RQ.Send(receivedMessage);
                System.Windows.Forms.MessageBox.Show("EnteredandSent2.");
                //Using Parallel.Invoke to send message to two different queues at the same time.
                //This is the best shot we have at parallel sending.
                //Parallel.Invoke(() =>{ Handler1RQ.Send(fwd); }, () => { Handler2RQ.Send(fwd); });

                #region set Handler Queue PropertyFilters
                Handler1RE.MessageReadPropertyFilter.Body = true;
                Handler1RE.MessageReadPropertyFilter.Label = true;
                Handler2RE.MessageReadPropertyFilter.Body = true;
                Handler2RE.MessageReadPropertyFilter.Label = true;
                #endregion

                #region Receiving from Handlers and setting message Formatters
                Message msg1 = Handler1RE.Receive();
                Message msg2 = Handler2RE.Receive();
                msg1.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                msg2.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                #endregion
                #region "BODY CORRELATION" if message from Handler1 is not correlated. 
                if (!msg1.Body.Equals(receivedMessage.Body)) //Checks if message from Handler1 is correlated.
                {
                    DisplayMessage(msg1);
                    System.Windows.Forms.MessageBox.Show("Not Same MSG1.");
                    break;
                }
                #endregion
                #region "BODY CORRELATION" if message from Handler2 is not correlated.
                if (!msg2.Body.Equals(receivedMessage.Body))
                {
                    DisplayMessage(msg2);
                    System.Windows.Forms.MessageBox.Show("Not Same MSG2.");
                    break;
                }
                #endregion

                System.Windows.Forms.MessageBox.Show("Correlated!");

                #region Handler1 has not received Acknowledgement
                if (processHandlerReply(msg1) != "ACK") //Deals with Handler1's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg1) == "ERR")
                    {
                        Response.Body = "Handler1";
                        Response.Label = "ERR";
                    }
                    else
                    {
                        Response.Body = "Handler1";
                        Response.Label = "UNKNOWN";
                    }
                    Response.ResponseQueue = IncomingQueue;
                    OutgoingQueue.Send(Response);
                    //continue;
                }
                #endregion
                #region Handler2 has not received Acknowledgement
                if (processHandlerReply(msg2) != "ACK") //Deals with Handler2's the received Acknowledgements or lack thereof
                {
                    if (processHandlerReply(msg2) == "ERR")
                    {
                        Response.Body = "Handler2";
                        Response.Label = "ERR";
                    }
                    else
                    {
                        Response.Body = "Handler2";
                        Response.Label = "UNKNOWN";
                    }
                    Response.ResponseQueue = IncomingQueue;
                    OutgoingQueue.Send(Response);
                }
                #endregion

                #region If all above are not satisfied -in order words- correlated and acknowledged
                Response.Label = "ACK";
                Response.ResponseQueue = IncomingQueue;
                OutgoingQueue.Send(Response);
                #endregion

                if (getBody(receivedMessage) == "STOP")
                {
                    loop = false;
                }
            }
        }
        string getBody(Message msg)
        {
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                string body = msg.Body.ToString();
                return body;
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error gettingbody-" + e + "CHEK14");
                return e.Message;
            }

        }
        void DisplayMessage(Message msg)
        {
            try
            {
                msg.Formatter = new XmlMessageFormatter(new String[] { "System.String,mscorlib" });
                string label = msg.Label.ToString();
                string body = msg.Body.ToString();
                System.Windows.Forms.MessageBox.Show(body);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error getting label and body-" + e + "CHEK15");
            }
        }
        public void publish(String msg, String label)
        {
            Message m = new Message();
            m.Body = msg;
            m.Label = label;
            multiRequestQueue.Send(m);
        }
        string processHandlerReply(Message msg)  //Returns TRUE if the label is ACK; Else False. 
        { //Assumes that the calling method has checked the CorrelationID
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
                System.Windows.Forms.MessageBox.Show("Error getting label and body-" + e + "CHEK5");
            }
            switch (label) //act based on the label Type
            {
                case "ACK": //successful events will fall in this category. This is the expected case.
                    {
                            System.Windows.Forms.MessageBox.Show(body + "-ACK-" + label);
                            return "ACK";
                    }
                case "NOTIF": //only used for "calibrated" notification, which is sent by handler in the beginning.
                    {
                        if (body == "calibrate")
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-CHEK10-" + label);
                            return "NOTIF";
                        }
                        else
                            return "ERR";
                    }
                case "EXCEPTION":
                    {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return "EXCEPTION";
                    }
                case "ERR":
                    {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return "ERR";
                       //In Future, handle errors in a better way. Add --- retries; "Retrying message: count x" 
                    }
                default: return "UNKNOWN";
            }
        }
        public void Purge()
        {
            IncomingQueue.Purge();
            OutgoingQueue.Purge();
        }
    }
}
