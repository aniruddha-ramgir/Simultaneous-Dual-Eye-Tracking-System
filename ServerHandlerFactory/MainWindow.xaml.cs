using System.Messaging;
using System.Windows;

namespace ServerHandlerFactory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FactoryFacade Factory;
        MessageQueue IncomingQueue = null;
        MessageQueue OutgoingQueue = null;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Start(object sender, RoutedEventArgs e)
        {
            Factory = new FactoryFacade();
            #region Incoming Queue
            if (MessageQueue.Exists(@".\Private$\SDET-RQ"))
            {
                IncomingQueue = new MessageQueue(@".\Private$\SDET-RQ");
            }
            else
            {
                MessageQueue.Create(@".\Private$\SDET-RQ");
                IncomingQueue = new MessageQueue(@".\Private$\SDET-RQ");
            }
            #endregion

            #region Outgoing Queue
            if (MessageQueue.Exists(@".\Private$\SDET-RE"))
            {
                OutgoingQueue = new MessageQueue(@".\Private$\SDET-RE");
            }
            else
            {
                MessageQueue.Create(@".\Private$\SDET-RE");
                OutgoingQueue = new MessageQueue(@".\Private$\SDET-RE");
            }
            #endregion
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            //Send notification to PsychoPy that SDET Handlers are shutting down
            //Gracefully exit Handlers i.e., send "stop" message to them. They have to save the files and exit themselves.
            //Keep SDET alive.
            //Factory.Observer.publish("stop","REQ");
        }
        private void send(object sender, RoutedEventArgs e)
        {
            this.fakeSend("ready", "REQ");
            //Factory.Begin();
        }
        public void fakeSend(string msg, string label)
        {
            Message m = new Message();
            m.Body = msg;
            m.Label = label;
            m.ResponseQueue = OutgoingQueue;
            IncomingQueue.Send(m);
        }
        private void incoming_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }
    }
}
