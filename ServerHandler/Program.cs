using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Configuration;

namespace paraprocess
{

    static class Program //Entry point
    {
        //public static EyeTribeAPIHandler Alpha;
        public static EyeTribeNonAPIHandler Alpha;

        public static bool launch(Int32 portnumber, string configPath,string serverPath)
        {
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Inside paraprocess.Program.launch()" + Environment.NewLine);
            //Alpha = new paraprocess.EyeTribeAPIHandler(portnumber, configPath, serverPath);
            Alpha = new paraprocess.EyeTribeNonAPIHandler(portnumber, configPath, serverPath);

            if (Alpha.ServerStarted != true)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Unable to start Eyetribe Server process #1 @paraprocess.Program.launch" + Environment.NewLine);
                return false;
            } 
            return true; //should return acknowledgement that processes have begun successfully
        }
    }

    class EyeTribeAPIHandler : IGazeListener, IServerHandler
    {
        //Thread HandlerWorker = null;
        #region gazeData Queues
        Queue<double[]> responseData1 = null;// = new Queue<double[]>();
        Queue<String> responseData2 = null;// = new Queue<String[]>();
       // Queue<String[]> responseData2 = null;// = new Queue<String[]>();
        const string featureNames = "port,timeStampString,rawX,rawY,smoothedX,smoothedY,isFixated,FrameState,"+
            "rawLeftX,rawLeftY,smoothedLeftX,smoothedLeftY,PupilCenterLeftX,PupilCenterLeftY,PupilSizeLeft,"+
            "RawRightX,rawRightY,smoothedRightX,smoothedRightY,PupilCenterRightX,PupilCenterRightY,PupilSizeRight,"+
            "TimeStampLong";
        const string breakLine = "break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break";
        #endregion

        #region Relevant variables
        public int _id { get; private set; }
        public Int32 _port { get; private set; }
        public string _sessionName { get;  set; }
        private string _cPath;
        private string _sPath;
        private string workingFilePath = null;
        #endregion

        #region Boolean variables
        public bool ServerStarted { get; private set; }
        public bool isTest=true;
        #endregion


        public EyeTribeAPIHandler(Int32 pNum, String cPath, String sPath)   //decides which device to start.Eg: device 0 or 1 or 2, etc
        {
            _port = pNum;
            _cPath = cPath;
            _sPath = sPath;
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Inside paraprocess.EyeTribeHandler Constructor." + Environment.NewLine);
            //HandlerWorker = new Thread(this.run);
            //HandlerWorker.Start();
        
          //  _id = 0; //default. Get ID from HandlerFacade in case if the project is extended to work with more than two trackers. Just pass the ID along with port to launch.
            /* if (IsServerProcessRunning() == true) //check if there is a server currently running
            {
                _id = 2;
                StartServerProcess(); //Start device 1 if there is already a server running.
            }
            else
            {
                _id = 1; */
                StartServerProcess();//start device 0 if no server is currently running
         //   }
            // Connect client
            //GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, "localhost", _port); // GazeManagerCore.ClientMode.Push is default

            //Initiliazing gazeData Queues
            responseData1 = new Queue<double[]>();
            responseData2 = new Queue<String>();
            //responseData2 = new Queue<String[]>();

            //If SDET test and main folders don't exist, the below line creates them.
            System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["testPath"]);
            System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["mainPath"]);
        }

        #region These methods deal with starting Servers and also implement IServerHandler
        private bool IsServerProcessRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "eyetribe")
                    {
                        return true;

                    }

                }
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @IsServerProcessRunning: " + e + Environment.NewLine);
                return false;
            }
            return false;
        }
        private void StartServerProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Normal; //set it to hidden 
            psi.FileName = _sPath;
            psi.Arguments = _cPath;
            if (string.IsNullOrEmpty(psi.FileName) || File.Exists(psi.FileName) == false)
            {
                ServerStarted = false;
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Starting Eye-Tribe server failed." + Environment.NewLine);
                return;
            }
            try
            {
                Process processServer = new Process();
                processServer.StartInfo = psi;
                processServer.Start();
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "Starting Eye-Tribe server success." + Environment.NewLine);
                ServerStarted = true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff", CultureInfo.InvariantCulture) + "An Exception has occured @StarServerrProcess: " + e + Environment.NewLine);
                ServerStarted = false;
            }
            Thread.Sleep(200); // wait for it to load
        }

        bool IServerHandler.IsServerProcessRunning()
        {
            throw new NotImplementedException();
        }

        void IServerHandler.StartServerProcess()
        {
            throw new NotImplementedException();
        }
        #endregion

        public bool IsListening()
        {
            if (GazeManager.Instance.HasGazeListener(this) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsActivated()
        {
            return GazeManager.Instance.IsActivated;
        }
        public bool IsCalibrated()
        {
            return GazeManager.Instance.IsCalibrated;
        }

        public bool preListening()
        {
            try
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "preListening() called." + Environment.NewLine);
                //string _name = Microsoft.VisualBasic.Interaction.InputBox("Please enter the name of the current experiment? This will be used to identify the current session", "Experiment Identifier");
                //Set path of the .csv file to be used
                if (isTest)
                    workingFilePath = ConfigurationManager.AppSettings["testPath"] + _sessionName + _port.ToString() + ".csv";
                else
                    workingFilePath = ConfigurationManager.AppSettings["mainPath"] + _sessionName + _port.ToString() + ".csv";

                //If the file doesn't exist, it creates a new file 
                //and appends "FeatureNames" string to it, as the first line

                File.AppendAllText(workingFilePath, featureNames + Environment.NewLine);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " .csv file created." + Environment.NewLine);
                return true;
            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An exception has occured @ StartListening()" + e + Environment.NewLine);
                return false;
            }

        }
        public void StartListening()
        {
            try
            {
                // Register this class for events
                GazeManager.Instance.AddGazeListener(this);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener added." + Environment.NewLine);
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An exception has occured @ StartListening()" + e + Environment.NewLine);
            }

        }
        public bool pauseListening()
        {
            if (!IsListening())
                return true;
            try
            {
                bool result = GazeManager.Instance.RemoveGazeListener(this);

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Pause.Listener removed. Waiting for gazeData queues to get emptied." + Environment.NewLine);
                
                //Waits until the data Queue is completely dequeued.
                SpinWait.SpinUntil(() => responseData1.Count == 0 && responseData2.Count == 0);

                File.AppendAllText(workingFilePath, breakLine + Environment.NewLine);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "GazeData Queues have been emptied." + Environment.NewLine);
                return result;
            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @ PauseListening()." + e + Environment.NewLine);
                return false;

            }
        }
        public void resumeListening()
        {
            try
            {
                // Register this class for events
                GazeManager.Instance.AddGazeListener(this);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Resumed.Listener added." + Environment.NewLine);

            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception @resumeListening()" +e+ Environment.NewLine);
            }
        }
        public bool StopListening()
        {
            if (!IsListening())
                return true;
            try
            {
                GazeManager.Instance.RemoveGazeListener(this);

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Waiting for gazeData queues to get emptied.@StopListening()" + Environment.NewLine);
                
                //Waits until the data Queue is completely dequeued.
                SpinWait.SpinUntil(() => responseData1.Count == 0 && responseData2.Count == 0);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "GazeData Queues have been emptied." + Environment.NewLine);

                return true;
            }
            catch(Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception received at @StopListening(): "+e + Environment.NewLine);
                return false;
            }
        }
        public bool Deactivate()
        {
            // If not activated dont deactivate.
            if (!IsActivated())
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener deactivated." + Environment.NewLine);
                return true;
            }
            GazeManager.Instance.Deactivate();
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener deactivated." + Environment.NewLine);
            return IsActivated();

        }

        public bool Connect()
        {
            // Connect client
            return GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, "localhost", _port); // GazeManagerCore.ClientMode.Push is default
        }
        public void OnGazeUpdate(GazeData gazeData)
        {
            responseData1.Enqueue(new double[]
                {
                #region General gazeData
                gazeData.RawCoordinates.X, gazeData.RawCoordinates.Y,
                gazeData.SmoothedCoordinates.X, gazeData.SmoothedCoordinates.Y,
                Convert.ToDouble(gazeData.IsFixated), //Coverting boolean to Double here. Watch out
                gazeData.State,
                #endregion

                #region LeftEye gazeData
                gazeData.LeftEye.RawCoordinates.X, gazeData.LeftEye.RawCoordinates.Y,
                gazeData.LeftEye.SmoothedCoordinates.X, gazeData.LeftEye.SmoothedCoordinates.Y,
                gazeData.LeftEye.PupilCenterCoordinates.X, gazeData.LeftEye.PupilCenterCoordinates.Y,
                gazeData.LeftEye.PupilSize,
                #endregion

                #region RightEye gazeData
                gazeData.RightEye.RawCoordinates.X,gazeData.RightEye.RawCoordinates.Y,
                gazeData.RightEye.SmoothedCoordinates.X,gazeData.RightEye.SmoothedCoordinates.Y,
                gazeData.RightEye.PupilCenterCoordinates.X,gazeData.RightEye.PupilCenterCoordinates.Y,
                gazeData.RightEye.PupilSize,
                #endregion

                gazeData.TimeStamp

                });
             responseData2.Enqueue(gazeData.TimeStampString);
            Task.Run(() => saveToFile());
        }
        void saveToFile()
        { 
            //add a locking mechanism to the queue, if necessary.
            File.AppendAllText(
                workingFilePath,
                _port.ToString() + "," + //port
                responseData2.Dequeue() + ","+ //timeStamp
                string.Join(",", responseData1.Dequeue()) + //gazeData
                Environment.NewLine
                );
            //File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Count of the Queue: " + responseData1.Count + Environment.NewLine);
        }

    }

    class EyeTribeNonAPIHandler :IServerHandler
    {
        #region relevant trings
        const string featureNames = "port,timeStampString,isFixated,FrameState,rawX,rawY,smoothedX,smoothedY," +
            "rawLeftX,rawLeftY,smoothedLeftX,smoothedLeftY,PupilCenterLeftX,PupilCenterLeftY,PupilSizeLeft," +
            "rawRightX,rawRightY,smoothedRightX,smoothedRightY,PupilCenterRightX,PupilCenterRightY,PupilSizeRight," +
            "TimeStampLong";

        const string breakLine = "break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break,break";

        const string REQ_HEATBEAT = "{\"category\":\"heartbeat\",\"request\":null}";
        const string REQ_PUSH = "{\"values\":{\"push\":true,\"version\":1},\"category\":\"tracker\",\"request\":\"set\"}";
        string response = string.Empty;
        #endregion

        #region Relevant variables
        public int _id { get; private set; }
        public Int32 _port { get; private set; }
        public string _sessionName { get; set; }
        private string _cPath;
        private string _sPath;
        private string workingFilePath = null;
        int exTimeLong = -1;

        private TcpClient socket;
        private Thread incomingThread;
        //StreamReader Globalreader = null;
        private System.Timers.Timer timerHeartbeat;
        #endregion

        #region Boolean variables
        public bool ServerStarted { get; private set; }
        public bool isTest = true;
        public bool isCalibrated = false; //IT is set by the Calibration-Handler

        bool isPaused = true;
        bool isStopped = true;
        bool isConnected = false;
        #endregion

        public EyeTribeNonAPIHandler(Int32 pNum, String cPath, String sPath)   //decides which device to start.Eg: device 0 or 1 or 2, etc
        {
            _port = pNum;
            _cPath = cPath;
            _sPath = sPath;

            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Inside paraprocess.EyeTribeNonAPIHandler Constructor." + Environment.NewLine);
            StartServerProcess();

            //If SDET test and main folders don't exist, the below line creates them.
            System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["testPath"]);
            System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["mainPath"]);
        }

        public bool IsListening()
        {
            return isPaused;
        } //CHANGE. 
        public bool IsActivated()
        {
            return socket.Connected;
        }
        public bool IsCalibrated()
        {
            return isCalibrated;
          /*  try
            {

                string REQ_isCalibrated = " {\"category\": \"tracker\",\"request\" : \"get\",\"values\": [ \"push\", \"iscalibrated\" ]}";
                Send(REQ_isCalibrated);
                StreamReader reader = new StreamReader(socket.GetStream());
                string response = reader.ReadLine();
                JObject jObject = JObject.Parse(response);

                if ("True".Equals((string)jObject.SelectToken("values.iscalibrated")))
                {
                    //System.Windows.Forms.MessageBox.Show((string)jObject.SelectToken("values.iscalibrated"));
                    File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Tracker calibrated" + Environment.NewLine);
                    return true;
                }
                else
                {
                    //remove below line
                    System.Windows.Forms.MessageBox.Show((string)jObject.SelectToken("values.iscalibrated"));
                    File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Tracker not calibrated" + Environment.NewLine);
                    return true; //CHANGE TO FALSE.
                }
                
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An exception has occured @IsCalibrated()" + e + Environment.NewLine);
                return false;
            } */
        }

        public bool preListening()
        {
            try
            {

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "preListening() called." + Environment.NewLine);
                
                //Set path of the .csv file to be used
                if (isTest)
                    workingFilePath = ConfigurationManager.AppSettings["testPath"] + _sessionName + _port.ToString() + ".csv";
                else
                    workingFilePath = ConfigurationManager.AppSettings["mainPath"] + _sessionName + _port.ToString() + ".csv";

                //If the file doesn't exist, it creates a new file and appends "FeatureNames" string to it, as the first line
                File.AppendAllText(workingFilePath, featureNames + Environment.NewLine);
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + " .csv file created." + Environment.NewLine);

                isPaused = false;
                isStopped = false;
               
                // Initiliaze a seperate thread to parse incoming data
                incomingThread = new Thread(ListenerLoop);
                incomingThread.Priority = ThreadPriority.AboveNormal;

                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An exception has occured @ PreListening()" + e + Environment.NewLine);
                return false;
            }

        }
        public void StartListening()
        {
            try
            {
                // Send the obligatory connect request message
                SendStartMessage(REQ_PUSH);
                //Start the thread that was intiatilised in PreListening()
                incomingThread.Start();

                #region heartbeat timer

                // Start a timer that sends a heartbeat every 250ms.
                // The minimum interval required by the server can be read out 
                // in the response to the initial connect request.   

                timerHeartbeat = new System.Timers.Timer(250);
                timerHeartbeat.Elapsed += delegate { Send(REQ_HEATBEAT); };
                timerHeartbeat.Start();
                #endregion

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener added." + Environment.NewLine);
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An exception has occured @ StartListening()" + e + Environment.NewLine);
            }
        }
        public bool pauseListening()
        {
            if (!IsListening())
                return true;

            try
            {

                timerHeartbeat.Stop();
                return isPaused = true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @ PauseListening()." + e + Environment.NewLine);
                return false;

            }
        }
        public void resumeListening()
        {
            try
            {
                SendStartMessage(REQ_PUSH);

                timerHeartbeat = new System.Timers.Timer(250);
                timerHeartbeat.Elapsed += delegate { Send(REQ_HEATBEAT); };
                timerHeartbeat.Start();

                isPaused = false;

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener resumed." + Environment.NewLine);

            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception @resumeListening()" + e + Environment.NewLine);
            }
        }
        public bool StopListening()
        {
            if (!IsListening())
                return true;
            try
            {
                timerHeartbeat.Stop();
                isStopped = true;
                isConnected = false;
                isCalibrated = false;
                //isPaused = true; //DONT SET IT TO TRUE, as ListenerLoop will wait endlessly for it turn to FALSE.
                //No need to close the socket here, because listenerloop will do it on its own.

                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Stopped. " + Environment.NewLine);

                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception received at @StopListening(): " + e + Environment.NewLine);
                return false;
            }
        }

        public bool Deactivate() //USELESS. DO NOTHING.
        {
            // If not activated dont deactivate.
            if (!IsActivated())
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener deactivated." + Environment.NewLine);
                return true;
            }
            File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Listener not deactived. ListenerLoop might be running." + Environment.NewLine);
            //return IsActivated();
            return true;
        }

        public bool Connect()
        {
            if (isConnected)
                return true;
            try
            {
                socket = new TcpClient("localhost", _port);
                return isConnected = true;
            }
            catch (Exception ex)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Error connecting: " + ex.Message + Environment.NewLine);
                return false;
            }
        }
        private void Send(string message)
        {
            if (socket != null && socket.Connected)
            {
                StreamWriter writer = new StreamWriter(socket.GetStream());
                writer.WriteLine(message);
                writer.Flush();
            }
        }
        private void SendStartMessage(string message)
        {
            if (socket != null && socket.Connected)
            {
                StreamWriter writer = new StreamWriter(socket.GetStream());

                //int now = DateTime.Now.Millisecond;
                //int MilliSecondsRemaining = 1000 - now; 

                // LOGIC => 1000 milliseconds = CURRENT MILLISECOND + REMAINING MILLISECONDS

                //Generally, 1000 milliseconds will be more than enough, but to avoid situations such as Now = 999th millisecond, use 2000
                //Create a timer that waits for the next "second" to start.
                var t = new Timer(o => { writer.WriteLine(message); }, null, 0, 1000 - DateTime.Now.Millisecond /*MilliSecondsRemaining*/ );

                //writer.WriteLine(message);
                writer.Flush();
            }
        }

        private void ListenerLoop()
        {
            using (StreamReader reader = new StreamReader(socket.GetStream()))
            {
                while (!isPaused) //&& !isStopped)
                {
                    try
                    {
                        response = string.Empty;
                        response = reader.ReadLine();
                        JObject jObject = JObject.Parse(response);

                        switch ((string)jObject["category"])
                        {
                            case "tracker":
                                {
                                    JToken values = jObject.GetValue("values");
                                    if (values != null) //is this necessary?
                                    {
                                        #region JObject variables
                                        JObject generalFrameData = JObject.Parse(values.SelectToken("frame").ToString());

                                        //Get LeftEye Data
                                        JObject leftEyeData = JObject.Parse(generalFrameData.SelectToken("lefteye").ToString());

                                        //Get RightEye Data
                                        JObject rightEyeData = JObject.Parse(generalFrameData.SelectToken("righteye").ToString());
                                        #endregion

                                        #region General Data

                                        string timeStampString = (string)generalFrameData.SelectToken("timestamp");
                                        int timeLong = (int)generalFrameData.SelectToken("time"); //int
                                        string isFixated = (string)generalFrameData.SelectToken("fix"); //bool
                                        string state = (string)generalFrameData.SelectToken("state"); //int

                                        #region Raw Coordinates
                                        //JObject rawGaze = JObject.Parse(generalFrameValues.SelectToken("raw").ToString());
                                        //double rawGazeX = (double)rawGaze.Property("x").Value;
                                        //double rawGazeY = (double)rawGaze.Property("y").Value;
                                        string rawGazeX = (string)generalFrameData.SelectToken("raw.x");
                                        string rawGazeY = (string)generalFrameData.SelectToken("raw.y");
                                        #endregion

                                        #region Smoothed Coordinates
                                        // JObject smoothedGaze = JObject.Parse(generalFrameValues.SelectToken("avg").ToString());
                                        string smoothedGazeX = (string)generalFrameData.SelectToken("avg.x");
                                        string smoothedGazeY = (string)generalFrameData.SelectToken("avg.y");
                                        #endregion

                                        #endregion

                                        #region LeftEye

                                        #region Raw Coordinates //double
                                        string rawGazeLeftX = (string)leftEyeData.SelectToken("raw.x");
                                        string rawGazeLeftY = (string)leftEyeData.SelectToken("raw.y");
                                        #endregion

                                        #region Smoothed Coordinates //double
                                        string smoothedGazeLeftX = (string)leftEyeData.SelectToken("avg.x");
                                        string smoothedGazeLeftY = (string)leftEyeData.SelectToken("avg.y");
                                        #endregion

                                        #region Pupil Data //double
                                        string pupilSizeLeft = (string)leftEyeData.SelectToken("psize");

                                        string pupilCenterLeftX = (string)leftEyeData.SelectToken("pcenter.x");
                                        string pupilCenterLeftY = (string)leftEyeData.SelectToken("pcenter.y");
                                        #endregion

                                        #endregion

                                        #region RightEye

                                        #region Raw Coordinates //double
                                        string rawGazeRightX = (string)rightEyeData.SelectToken("raw.x");
                                        string rawGazeRightY = (string)rightEyeData.SelectToken("raw.y");
                                        #endregion

                                        #region Smoothed Coordinates //double
                                        string smoothedGazeRightX = (string)rightEyeData.SelectToken("avg.x");
                                        string smoothedGazeRightY = (string)rightEyeData.SelectToken("avg.y");
                                        #endregion

                                        #region Pupil Data //double
                                        string pupilSizeRight = (string)rightEyeData.SelectToken("psize");

                                        string pupilCenterRightX = (string)rightEyeData.SelectToken("pcenter.x");
                                        string pupilCenterRightY = (string)rightEyeData.SelectToken("pcenter.y");
                                        #endregion


                                        #endregion

                                        #region Marking lapses
                                        double lapse;
                                        if (exTimeLong != -1 && (lapse = timeLong - exTimeLong) > 17) //if there is a lapse of duration X, such that 30>X>18
                                        {
                                            if (30 >= (lapse = timeLong - exTimeLong))
                                            {
                                                lapse = 1;
                                            }
                                            else
                                            {
                                                lapse = lapse / 17;
                                            }
                                            for (int i = 0; i < lapse; i++) //prints "lapse" depending on the no.of data-points that have been skipped.
                                            {
                                                File.AppendAllText(
                                                               workingFilePath,
                                                               _port.ToString() + "," +
                                                               "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," +
                                                               "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," +
                                                               "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" + "," +
                                                               "0" + Environment.NewLine);
                                            }

                                        }
                                        #endregion

                                        #region print gazedata to file
                                        File.AppendAllText(
                                                        workingFilePath,
                                                        _port.ToString() + "," +
                                                        timeStampString + "," + isFixated + "," + state + "," + rawGazeX + "," + rawGazeY + "," + smoothedGazeX + "," + smoothedGazeY + "," +
                                                        rawGazeLeftX + "," + rawGazeLeftY + "," + smoothedGazeLeftX + "," + smoothedGazeLeftY + "," + pupilCenterLeftX + "," + pupilCenterLeftY + "," + pupilSizeLeft + "," +
                                                        rawGazeRightX + "," + rawGazeRightY + "," + smoothedGazeRightX + "," + smoothedGazeRightY + "," + pupilCenterRightX + "," + pupilCenterRightY + "," + pupilSizeRight + "," +
                                                        timeLong.ToString() + Environment.NewLine);

                                        //exTimeStamp gives the timestamp of the last accessed data-point.
                                        //update the exTimeStamp with the current TimeStamp
                                        exTimeLong = timeLong;
                                        #endregion
                                    }
                                    else
                                    //If Eye-Tribe server replies anything, then it might not contain any values. This is handle such cases.
                                    {
                                        #region Write the message received to the LOG file - OR -print null to file if values = null.
                                        File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Received a 'null' message from the Stream: " + response + Environment.NewLine);

                                         File.AppendAllText(
                                                             workingFilePath,
                                                             _port.ToString() + "," +
                                                             "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," +
                                                             "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," +
                                                             "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," +
                                                             "null" + Environment.NewLine); 

                                        #endregion
                                    }
                                    continue;
                                }
                            case "heartbeat":
                                {
                                    continue;
                                }
                        }
                        if (reader.EndOfStream)
                        {
                            if (isStopped)
                            {
                                //If it has stopped
                                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "All data-points were read and saved to file. Exiting ServerHandler " + Environment.NewLine);
                                break;
                            }
                            File.AppendAllText(workingFilePath, breakLine + Environment.NewLine);
                            //If the program was 'PAUSE'-ed, then isPaused == TRUE.
                            //So the program Waits until isPaused is FALSE
                            SpinWait.SpinUntil(() => isPaused = false);
                        }
                    }
                    catch (Exception e)
                    {
                        File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Exception occured while reading data from Stream: " + e + Environment.NewLine);
                    }
                }
            }
        }

        #region These methods deal with starting Servers and also implement IServerHandler
        public bool IsServerProcessRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "eyetribe")
                    {
                        return true;

                    }

                }
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @IsServerProcessRunning: " + e + Environment.NewLine);
                return false;
            }
            return false;
        }
        public void StartServerProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Minimized; //set it to hidden 
            psi.FileName = _sPath;
            psi.Arguments = _cPath;
            if (string.IsNullOrEmpty(psi.FileName) || File.Exists(psi.FileName) == false)
            {
                ServerStarted = false;
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Starting Eye-Tribe server failed." + Environment.NewLine);
                return;
            }
            try
            {
                Process processServer = new Process();
                processServer.StartInfo = psi;
                processServer.Start();
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "Starting Eye-Tribe server success." + Environment.NewLine);
                ServerStarted = true;
            }
            catch (Exception e)
            {
                File.AppendAllText(ServerHandler.HandlerFacade.logFilePathName, DateTime.Now.ToString("hh.mm.ss.ffffff") + "An Exception has occured @StarServerrProcess: " + e + Environment.NewLine);
                ServerStarted = false;
            }
            Thread.Sleep(200); // wait for it to load
        }
        #endregion
    }
}
