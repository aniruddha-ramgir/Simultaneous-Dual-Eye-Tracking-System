namespace paraprocess
{
    interface IServerHandler //Implement this to add Servers for Trackers of different brand 
    {
        bool IsServerProcessRunning();
        void StartServerProcess();

        bool IsListening();
        bool IsCalibrated();

        void StartListening();
        bool pauseListening();
        bool StopListening();
        bool Deactivate();
    }
}
