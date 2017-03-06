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
        string body, label;
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
        
        private void send(object sender, RoutedEventArgs e)
        {
             this.fakeSend(body.ToLower(), label.ToUpper());
            /*
            Factory.ExitServerHandlers();
            Message msg = new Message();
            msg = OutgoingQueue.Receive();
            msg.Formatter = new XmlMessageFormatter(new System.String[] { "System.String,mscorlib" });
            if (msg.Body.ToString() == "exit" && msg.Label.ToString() == "ACK")
            {
                //When an acknowledgment is received, we can close the proccesses.
                Factory.ExitServerHandlers();
            }
            */
        }
        public void fakeSend(string msg, string label)
        {
            Message m = new Message();
            m.Body = msg;
            m.Label = label;
            //m.ResponseQueue = OutgoingQueue;
            IncomingQueue.Send(m);
        }
        private void incoming_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            body = incoming.Text;
        }

        private void incoming_Copy_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            label = incoming_Copy.Text;
        }
        private void WindowClosed(object sender, System.EventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
