using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using System.Collections.Generic;

namespace paraprocess
{

    static class Program //Entry point
    {
        public static EyeTribeHandler Alpha;
        public static bool launch(Int32 portnumber, string configPath,string serverPath)
        {
            Alpha = new paraprocess.EyeTribeHandler(portnumber,configPath,serverPath);
            if (Alpha.ServerStarted != true)
            {
                Console.WriteLine("Unable to start Eyetribe Server process #1");
                return false; 
            }
            return true; //should return acknowledgement that processes have begun successfully
        }

        public static void record() //USELESS. REMOVE IT
        {
            Alpha.StartListening();
            Thread.Sleep(39000); // simulate app lifespan (e.g. OnClose/Exit event)
            Alpha.Deactivate();
        }
    }
    class EyeTribeHandler : IServerHandler, IGazeListener
    {
        Queue<double[]> responseData = new Queue<double[]>();
        string[] featureNames = new string[23] 
        {
            "port","timeStampString","rawX","rawY","smoothedX","smoothedY","isFixated","FrameState",
            "rawLeftX","rawLeftY","smoothedLeftX","smoothedLeftY","PupilCenterLeftX","PupilCenterLeftY","PupilSizeLeft",
            "RawRightX","rawRightY","smoothedRightX","smoothedRightY","PupilCenterRightX","PupilCenterRightY","PupilSizeRight","TimeStampLong"
        };
        //double[] dataRow = new double[22];
        public int _id { get; private set; }
        public bool ServerStarted { get; private set; }
        public Int32 _port { get; private set; }
        private string _cPath;
        private string _sPath;
        
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
            // Connect client
            GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, "localhost", _port); // GazeManagerCore.ClientMode.Push is default

            //responseData.Enqueue();
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
        public void StartListening()
        {
            // Register this class for events
            GazeManager.Instance.AddGazeListener(this);
        }
        public bool StopListening()
        {
            return GazeManager.Instance.RemoveGazeListener(this);
        }
        public bool IsActivated()
        {
            return GazeManager.Instance.IsActivated;
        }
        public bool IsCalibrated()
        {
            return GazeManager.Instance.IsCalibrated;
        }
        public bool Deactivate()
        {
            // Disconnect client
            GazeManager.Instance.Deactivate();
            if (IsActivated()!= false)
            {
                return true;
            }
            return false;
        }
        public void OnGazeUpdate(GazeData gazeData)
        {
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

            //responseData.Enqueue(new double[] { rawX, rawY, smoothX, smoothY, fixationState, Framestate, rawLX, rawLY, smoothLX, smoothLY, pupilCenterLX, pupilCenterLY, pupilSizeL, rawRX, rawRY, smoothRX, smoothRY, pupilCenterRX, pupilCenterRY, pupilSizeR, time});
            //Write " responseData.Dequeue(); " to an appropriate csv file.
            //Console.WriteLine(smoothX + "," + smoothY + "@" + timeString);
            // Move point, do hit-testing, log coordinates etc.
        }
        private bool IsServerProcessRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "eyetribe")
                        return true;
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
            psi.WindowStyle = ProcessWindowStyle.Minimized; //set it to hidden 
            psi.FileName = _sPath;
            //if (dev_number == 1)
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
    }
}
