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
                StartHandlerProcess("6555");
            //No need to wait here because, Handler waits internally after starting the server.
            StartHandlerProcess("6556");
            Observer = new FactoryObserver("6555", "6556");
            Observer.Run();
            //Observer.Purge();
        }
        private void StartHandlerProcess(string port)
        { 
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Minimized; //set it to hidden 
            psi.FileName = GetHandlerExecutablePath();
            psi.Arguments = port;  //start with device 1 if its already running. //???

            if (psi.FileName == string.Empty || File.Exists(psi.FileName) == false)
            {
                FactoryStarted = false;
                return;
            }
            try
            {
                Process processServer = new Process();
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
    }
    class FactoryObserver
    {
        #region Declaration statements for Message and MessageQueue Object
        //SDET Queue
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;

        //MULITCAST Queue. Handlers will receive requests from this queue.
        MessageQueue multiRequestQueue = null;

        //Response Queues. Read messages from RE.
        MessageQueue Handler1RE = null;
        MessageQueue Handler2RE = null;

        //Message to be sent back to PsychoPy
        Message Response = null;

        //Message to be forwarded
        Message fwd = null;
        #endregion

        #region Boolean Identifiers that tell us if we have received any message from their respective senders.
        bool Handler1Received = false;
        bool Handler2Received = false;
        bool IncomingReceived = false;
        #endregion

        public FactoryObserver(string port1,string port2)
        {
            //string incomingQueueName = "SDET-RQ";
            //outgoingQueueName = "SDET-RE";
            try
            {
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

                #region Multicast Queue
                multiRequestQueue = new MessageQueue("FormatName:MULTICAST=234.1.1.1:8001");
                #endregion

                //We do not have to initialize Request/Incoming queues for Handler processes.
                //Because we will be sending to the MULTI-CAST address.
                //Processes can create their own RQ and RE queues.
                //We are initialising RE queues here JUST TO BE SAFE.

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
        public async void Run()
        {
            //SDET starts with a "calibrate" NOTIFICATION from the Handlers.
            Task listenHandler1 = Task.Factory.StartNew(listenHandler1RE);
            Task listenHandler2 = Task.Factory.StartNew(listenHandler2RE);
            await Task.WhenAll(listenHandler1, listenHandler2);

            //Waits until "calibrate" is received.
            SpinWait.SpinUntil(() => Handler1Received && Handler2Received);

            //This statement is not necessary and is redundant, but for now I'm keeping it here to double check
            if (Handler1Received == true && Handler2Received == true)
            {
                System.Windows.Forms.MessageBox.Show("success1.");
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("failure1."+Handler1Received.ToString()+Handler2Received.ToString());
            }

            //Start the loop of listening and forwarding.
            while (true)
            {
                System.Windows.Forms.MessageBox.Show("Entered1." + Handler1Received.ToString() + Handler2Received.ToString());
                //Reset Tasks.
                listenHandler1 = null;
                listenHandler2 = null;

                //reset values so that next messages can use it again.
                Handler1Received = Handler2Received = IncomingReceived= false;

                //gets id from FWD message and sets corrID to RESPONSE. And then multicasts FWD
                listenIncomingQueue();
                SpinWait.SpinUntil(() => IncomingReceived);
                listenHandler1 = Task.Factory.StartNew(listenHandler1RE);
                listenHandler2 = Task.Factory.StartNew(listenHandler2RE);
                await Task.WhenAll(listenHandler1, listenHandler2);
                System.Windows.Forms.MessageBox.Show("Entered2." + Handler1Received.ToString() + Handler2Received.ToString());

                //Waits until "calibrate" is received.
                SpinWait.SpinUntil(() => Handler1Received && Handler2Received);

                System.Windows.Forms.MessageBox.Show("Entered3." + Handler1Received.ToString() + Handler2Received.ToString());

                while (Handler1Received != true && Handler2Received != true)
                {
                    await Task.Delay(50);
                }
                if (Handler1Received == true && Handler2Received == true)
                {
                    OutgoingQueue.Send(Response); //send success reply to PsychoPy
                    System.Windows.Forms.MessageBox.Show("success.");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("failure." + Handler1Received.ToString() + Handler2Received.ToString());
                }
            }
        }
        public void publish(String msg, String label)
        {
            Message m = new Message();
            m.Body = msg;
            m.Label = label;
            multiRequestQueue.Send(m);
        }
        private void listenIncomingQueue()
        {
            try
            {
                IncomingQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(IncomingReceiveCompleted);

                // Begin the asynchronous receive operation.
                IncomingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK2");
            }
        }
        void listenHandler1RE()
        {
            try
            {
                Handler1RE.ReceiveCompleted += new ReceiveCompletedEventHandler(Handler1ReceiveCompleted);

                // Begin the asynchronous receive operation.
                Handler1RE.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK3");
                //m.Body = e.Message;
                //m.Label = "EXCEPTION";
            }
        }
        void listenHandler2RE()
        {
            try
            {
                Handler2RE.ReceiveCompleted += new ReceiveCompletedEventHandler(Handler2ReceiveCompleted);

                // Begin the asynchronous receive operation.
                Handler2RE.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK4");
                //m.Body = e.Message;
                //m.Label = "EXCEPTION";
            }
        }
        bool processHandlerMessage(Message msg)  //Returns TRUE if the label is ACK; Else False. 
        { //Assumes that the calling method has checked the CorrelationID
            string label = null;
            string body = null;
            try
            {
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
                        if (Response.CorrelationId == msg.CorrelationId)
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-ACK-" + label);
                            return true;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-ACK-" + label);
                            return false;
                        }
                    }
                case "NOTIF": //only used for "calibrated" notification, which is sent by handler in the beginning.
                    {
                        if (body == "calibrate")
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-CHEK10-" + label);
                            return true;
                        }
                        else
                            return false;
                    }
                case "EXCEPTION":
                    {
                        if (Response.CorrelationId == msg.CorrelationId)
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return true;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return false;
                        }
                    }
                case "ERR":
                    {
                        if (Response.CorrelationId == msg.CorrelationId)
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return true;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(body + "-EXCPTN-" + label);
                            return false;
                        } //In Future, handle errors in a better way. Add --- retries; "Retrying message: count x" 
                    }
                default: return false;
            }
        }
        private void IncomingReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {
            //Message receivedMessage = null;
            IncomingReceived = false;
            MessageQueue workingQueue = null;
            try
            {
                // Connect to the queue.
                workingQueue = (MessageQueue)source;

                System.Windows.Forms.MessageBox.Show("Triggered");
                // End the asynchronous receive operation.
                fwd = workingQueue.EndReceive(asyncResult.AsyncResult);

                // Process and forward Message
                Response = fwd;
                Response.CorrelationId = fwd.Id;
                Response.ResponseQueue = IncomingQueue;
                multiRequestQueue.Send(fwd);
                IncomingReceived = true;
                // Restart the asynchronous receive operation.
               // workingQueue.BeginReceive();
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK6");
            }
        }
        private void Handler1ReceiveCompleted(object source, ReceiveCompletedEventArgs asyncResult)
        {
            Handler1Received = false;
            Message m = new Message();
            MessageQueue workingQueue = null;
            try
            {
                // Connect to the queue.
                workingQueue = (MessageQueue)source;
                System.Windows.Forms.MessageBox.Show("Triggered1");

                // End the asynchronous receive operation.
                m = workingQueue.EndReceive(asyncResult.AsyncResult);

                // Check if the message, which was received from Handler1, has the same correlation ID of the "forwarded" message.
 
                    //processHandlerMessage(m);
                if (processHandlerMessage(m) == true)
                    Handler1Received = true;
                else
                    Handler1Received = false;
                // Restart the asynchronous receive operation.
               // workingQueue.BeginReceive();

            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message +"-"+e.Source+"CHEK7");
            }
        }
        private void Handler2ReceiveCompleted(Object source, ReceiveCompletedEventArgs asyncResult)
        {
            Handler2Received = false;
            Message m = new Message();
            MessageQueue workingQueue = null;
            try
            {
                // Connect to the queue.
                workingQueue = (MessageQueue)source;

                System.Windows.Forms.MessageBox.Show("Triggered3");
                // End the asynchronous receive operation.
                m = workingQueue.EndReceive(asyncResult.AsyncResult);
                if (processHandlerMessage(m) == true)
                {
                    Handler2Received = true;
                }
                else
                    Handler2Received = false;
                // Check if the message, which was received from Handler1, has the same correlation ID of the "forwarded" message.
                /*  if (Response.CorrelationId == m.CorrelationId)
                  { 
                      Handler2Received = processHandlerMessage(m);
                  }
                  else
                  {
                      Handler2Received = false;
                  } */
                // Restart the asynchronous receive operation.
                //workingQueue.BeginReceive();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + "CHEK8");
            }
        }
        public void Purge()
        {
            IncomingQueue.Purge();
            OutgoingQueue.Purge();
        }
    }
}
