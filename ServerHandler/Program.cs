using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using System.Collections.Generic;
using ServerHandler.Properties;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace paraprocess
{

    static class Program //Entry point
    {
        public static EyeTribeHandler Alpha;
        public static bool launch(Int32 portnumber, string configPath,string serverPath)
        {
            Alpha = new paraprocess.EyeTribeHandler(portnumber, configPath, serverPath);
            if (Alpha.ServerStarted != true)
            {
                MessageBox.Show("Unable to start Eyetribe Server process #1");
                return false;
            } 
            return true; //should return acknowledgement that processes have begun successfully
        }
        public static void exit()
        {
            Calibration.MainWindow.exit();
            //Environment.Exit(0);
            Process.GetCurrentProcess().Kill();
            //System.Windows.Application.Current.Shutdown(); 
        } 
    }
    class EyeTribeHandler : IServerHandler, IGazeListener
    {
        #region gazeData Queues
        Queue<double[]> responseData1 = null;// = new Queue<double[]>();
        Queue<String[]> responseData2 = null;// = new Queue<String[]>();
        string[] featureNames = new string[23]
        {
            "port","timeStampString","rawX","rawY","smoothedX","smoothedY","isFixated","FrameState",
            "rawLeftX","rawLeftY","smoothedLeftX","smoothedLeftY","PupilCenterLeftX","PupilCenterLeftY","PupilSizeLeft",
            "RawRightX","rawRightY","smoothedRightX","smoothedRightY","PupilCenterRightX","PupilCenterRightY","PupilSizeRight","TimeStampLong"
        };
        #endregion

        #region Relevant variables
        public int _id { get; private set; }
        public Int32 _port { get; private set; }
        private string _cPath;
        private string _sPath;
        private string workingFilePath = null;
        #endregion

        #region Boolean variables
        public bool ServerStarted { get; private set; }
        public bool isTest=true;
        #endregion

        #region Stream Files
        private FileStream _file = null;
        private StreamWriter _fileOut = null;
        #endregion

        public EyeTribeHandler(Int32 pNum, String cPath, String sPath)   //decides which device to start.Eg: device 0 or 1 or 2, etc
        {
            _id = 0; //default. Get ID from HandlerFacade in case if the project is extended to work with more than two trackers. Just pass the ID along with port to launch.
            _port = pNum;
            _cPath = cPath;
            _sPath = sPath;
            if (IsServerProcessRunning() == true) //check if there is a server currently running
            {
                _id = 2;
                StartServerProcess(); //Start device 1 if there is already a server running.
            }
            else
            {
                _id = 1;
                StartServerProcess();//start device 0 if no server is currently running
            }
            //Initiliazing gazeData Queues
            responseData1 = new Queue<double[]>();
            responseData2 = new Queue<String[]>();

            // Connect client
            GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, "localhost", _port); // GazeManagerCore.ClientMode.Push is default

            //If SDET test and main folders don't exist, the below line creates automatically.
            System.IO.Directory.CreateDirectory(Resources.testPath);
            System.IO.Directory.CreateDirectory(Resources.mainPath);
        }
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
        public void StartListening()
        {
            try
            {
                string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter the name of the current experiment? This will be used to identify the current session", "Experiment Identifier");
                //Set path of the .csv file to be used
                if (!isTest)
                    workingFilePath = Resources.testPath + name + _port.ToString() + ".csv";
                else
                    workingFilePath = Resources.mainPath + name + _port.ToString() + ".csv";

                _file = new FileStream(workingFilePath, FileMode.OpenOrCreate);
                _fileOut = new StreamWriter(_file);

                //Add feature Names to the first line of the file
                _fileOut.WriteLine(featureNames);

                // Register this class for events
                GazeManager.Instance.AddGazeListener(this);
            }
            catch (Exception e)
            {
                MessageBox.Show("CHEK20" + e);
            }

        }
        public bool StopListening()
        {
            bool result = GazeManager.Instance.RemoveGazeListener(this);
            MessageBox.Show("SpinWait Begins.CHEK21");
            //Waits until the data Queue is completely dequeued.
            SpinWait.SpinUntil(() => responseData1.Count== 0 && responseData2.Count == 0);
            //And then itt waits until streamWriter is empty
            SpinWait.SpinUntil(() => _fileOut==null);
            MessageBox.Show("SpinWait completed.CHEK22");
            //Close StreamWriter
            _fileOut.Close();
            //Close FileStream.
            _file.Close();

            return result;
        }
        public bool pauseListening()
        {
            try
            {
                bool result = GazeManager.Instance.RemoveGazeListener(this);

                //Waits until the data Queue is completely dequeued.
                SpinWait.SpinUntil(() => responseData1 == null && responseData2 == null);
                //And then it waits until streamWriter is empty
                SpinWait.SpinUntil(() => _fileOut == null);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Deactivate()
        {
            // Disconnect client
            GazeManager.Instance.Deactivate();
            if (IsActivated() != false)
            {
                return true;
            }
            return false;
        }
        public void OnGazeUpdate(GazeData gazeData)
        {
            #region USELESS code - Copying from gazeData object to variables then enqueue-ing them
            /* 
            #region General Data -7
            String timeString = gazeData.TimeStampString;
            long timeLong = gazeData.TimeStamp;
            double rawX = gazeData.RawCoordinates.X;
            double rawY = gazeData.RawCoordinates.Y;
            double smoothX = gazeData.SmoothedCoordinates.X;
            double smoothY = gazeData.SmoothedCoordinates.Y;
            double fixationState = Convert.ToDouble(gazeData.IsFixated); //saving bool as double.
            int Framestate = gazeData.State;
            #endregion

            #region LeftEye Data -7
            double rawLX = gazeData.LeftEye.RawCoordinates.X;
            double rawLY = gazeData.LeftEye.RawCoordinates.Y;
            double smoothLX = gazeData.LeftEye.SmoothedCoordinates.X;
            double smoothLY = gazeData.LeftEye.SmoothedCoordinates.Y;
            double pupilCenterLX = gazeData.LeftEye.PupilCenterCoordinates.X;
            double pupilCenterLY = gazeData.LeftEye.PupilCenterCoordinates.Y;
            double pupilSizeL = gazeData.LeftEye.PupilSize;
            #endregion

            #region RightEye Data -7
            double rawRX = gazeData.RightEye.RawCoordinates.X;
            double rawRY = gazeData.RightEye.RawCoordinates.Y;
            double smoothRX = gazeData.RightEye.SmoothedCoordinates.X;
            double smoothRY = gazeData.RightEye.SmoothedCoordinates.Y;
            double pupilCenterRX = gazeData.RightEye.PupilCenterCoordinates.X;
            double pupilCenterRY = gazeData.RightEye.PupilCenterCoordinates.Y;
            double pupilSizeR = gazeData.RightEye.PupilSize;
            #endregion

            responseData1.Enqueue(new double[] { rawX, rawY, smoothX, smoothY, fixationState, Framestate, rawLX, rawLY, smoothLX, smoothLY, pupilCenterLX, pupilCenterLY, pupilSizeL, rawRX, rawRY, smoothRX, smoothRY, pupilCenterRX, pupilCenterRY, pupilSizeR, timeLong });
            */
            #endregion

            responseData2.Enqueue(new String[] { _port.ToString(), gazeData.TimeStampString });
            responseData1.Enqueue(new double[]
                {
                #region General gazeData
                gazeData.RawCoordinates.X, gazeData.RawCoordinates.Y,
                gazeData.SmoothedCoordinates.X, gazeData.SmoothedCoordinates.Y,
                Convert.ToDouble(gazeData.IsFixated),
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
            Task.Run(() => saveToFile());
            //Write " responseData.Dequeue(); " to an appropriate csv file.
            //Console.WriteLine(smoothX + "," + smoothY + "@" + timeString);
        }
        void saveToFile()
        {
                _fileOut.WriteLine
                    (
                        string.Join(",",responseData1.Dequeue()) + string.Join(", ", responseData2.Dequeue())
                    );
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
                Console.WriteLine(e);
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
                //psi.Arguments = "C:\\Users\\Aniruddha\\AppData\\Local\\EyeTribe\\config.cfg"; //should vary for each tracker. This should be stored in the software package
            if (psi.FileName == string.Empty || File.Exists(psi.FileName) == false)
            {
                ServerStarted = false;
                return;
            }
            try
            {
                Process processServer = new Process();
                processServer.StartInfo = psi;
                processServer.Start();
                ServerStarted = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
    }
}
