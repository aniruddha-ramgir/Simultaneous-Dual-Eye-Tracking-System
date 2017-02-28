using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace paraprocess
{
    interface IServerHandler //Implement this to add Servers for Trackers of different brand 
    {
        bool IsServerProcessRunning();
        void StartServerProcess();
        bool IsListening();
        void StartListening();
        bool StopListening();
        bool Deactivate();
        bool IsCalibrated();
        //bool ServerStarted(){};
    }
}
