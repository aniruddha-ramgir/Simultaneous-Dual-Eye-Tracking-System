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
        string body = null, label = null;
        MessageQueue IncomingQueue = null;
       // MessageQueue OutgoingQueue = null;

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

         /*   #region Outgoing Queue
            if (MessageQueue.Exists(@".\Private$\SDET-RE"))
            {
                OutgoingQueue = new MessageQueue(@".\Private$\SDET-RE");
            }
            else
            {
                MessageQueue.Create(@".\Private$\SDET-RE");
                OutgoingQueue = new MessageQueue(@".\Private$\SDET-RE");
            }
            #endregion */
        }
        
        private void send(object sender, RoutedEventArgs e)
        {
             this.fakeSend(body.ToLower(), label.ToUpper());
        }

        public void fakeSend(string message, string label)
        {
            Message m = new Message();
            m.Body = message;
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This application was developed by Aniruddha Ramgir at Action Control and Cognition Lab, University of Hyderabad. " 
                + System.Environment.NewLine 
                + "Would you like to visit our lab website?"
                , "About us", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    System.Diagnostics.Process.Start("https://actioncontrolcognition.wordpress.com/");
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/aniruddha-ramgir/Simultaneous-Dual-Eye-Tracking-System");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);

        }
    }
}
