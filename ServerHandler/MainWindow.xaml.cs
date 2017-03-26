/*
 * Copyright (c) 2013-present, The Eye Tribe. 
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree. 
 *
 */
using System;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using EyeTribe.Controls.Calibration;
using EyeTribe.Controls.TrackBox;
using EyeTribe.ClientSdk.Data;
using System.Windows.Interop;
using EyeTribe.ClientSdk;
using MessageBox = System.Windows.MessageBox;

namespace Calibration
{
    public partial class MainWindow : IConnectionStateListener
    {
        private Screen activeScreen = Screen.PrimaryScreen;

        //private bool isCalibrated = false;
        private bool validResult = false;

        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += (sender, args) => InitClient();
           // this.KeyDown += MainWindow_KeyDown;
        }

        private void InitClient()
        {
            // Activate/connect client
             GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, "localhost", paraprocess.Program.Alpha._port);


            // Listen for changes in connection to server
            GazeManager.Instance.AddConnectionStateListener(this);
            port.Text = Convert.ToString(paraprocess.Program.Alpha._port, System.Globalization.CultureInfo.InvariantCulture);

            // Fetch current status
            OnConnectionStateChanged(GazeManager.Instance.IsActivated);

            // Add a fresh instance of the trackbox in case we reinitialize the client connection.
            TrackingStatusGrid.Children.Clear();
            TrackingStatusGrid.Children.Add(new TrackBoxStatus());

            UpdateState();

            //REMOVE THIS
            GazeManager.Instance.Deactivate();
            ServerHandler.HandlerFacade.Observer.sendResponse("calibrate", "NOTIF");
        }

        public void OnConnectionStateChanged(bool IsActivated)
        {
            // The connection state listener detects when the connection to the EyeTribe server changes
            if (btnCalibrate.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new MethodInvoker(() => OnConnectionStateChanged(IsActivated)));
                return;
            }

            if (!IsActivated)
                GazeManager.Instance.Deactivate();

            UpdateState();
        }

        private void ButtonCalibrateClicked(object sender, RoutedEventArgs e)
        {
            // Check connectivitiy status
            if (GazeManager.Instance.IsActivated == false)
                InitClient();

            // API needs to be active to start calibrating
            if (GazeManager.Instance.IsActivated)
                Calibrate();
            else
                UpdateState(); // show reconnect
        }

        private void Calibrate()
        {
            // Update screen to calibrate where the window currently is
            activeScreen = Screen.FromHandle(new WindowInteropHelper(this).Handle);

            // Initialize and start the calibration
            CalibrationRunner calRunner = new CalibrationRunner(activeScreen, activeScreen.Bounds.Size, 9);
            calRunner.OnResult += calRunner_OnResult;
            calRunner.Start();
        }

        private void calRunner_OnResult(object sender, CalibrationRunnerEventArgs e)
        {
            // Invoke on UI thread since we are accessing UI elements
            if (RatingText.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new MethodInvoker(() => calRunner_OnResult(sender, e)));
                return;
            }
            switch (e.Result)
            {
                case CalibrationRunnerResult.Success:
                    {
                        //isCalibrated = true;
                        UpdateState();

                        DialogResult result1 = System.Windows.Forms.MessageBox.Show(
                                 "Calibration Result of:" + port.Text.ToString()+" was "+ RatingFunction(GazeManager.Instance.LastCalibrationResult),
                                 "Click YES to accept the result. NO to discard.",
                                  MessageBoxButtons.YesNo);

                        if (result1 == System.Windows.Forms.DialogResult.Yes && validResult)
                        {
                            //Send message that the tracker is calibrated    
                            ServerHandler.HandlerFacade.Observer.sendResponse("calibrate", "NOTIF");
                            paraprocess.Program.Alpha.isCalibrated = true;
                        }
                        break;
                    }

                case CalibrationRunnerResult.Failure:
                    {
                        MessageBox.Show(this, "Calibration failed. Reason: " + e.Message);
                        break;
                    }
                case CalibrationRunnerResult.Abort:
                    {
                        MessageBox.Show(this, "The calibration was aborted. Reason: " + e.Message);
                        break;
                    }
                case CalibrationRunnerResult.Error:
                    {
                        MessageBox.Show(this, "An error occured during calibration. Reason: " + e.Message);
                        break;
                    }
                case CalibrationRunnerResult.Unknown:
                    {
                        MessageBox.Show(this, "Calibration exited with unknown state. Reason: " + e.Message);
                        break;
                    }
            }
        }

        private void UpdateState()
        {
            // No connection
            if (GazeManager.Instance.IsActivated == false)
            {
                btnCalibrate.Content = "Connect";
                RatingText.Text = "";
                return;
            }

            if (GazeManager.Instance.IsCalibrated == false)
            {
                btnCalibrate.Content = "Calibrate";
            }
            else
            {
                btnCalibrate.Content = "Recalibrate";

                if (GazeManager.Instance.LastCalibrationResult != null)
                    RatingText.Text = RatingFunction(GazeManager.Instance.LastCalibrationResult);
            }
        }

        private string RatingFunction(CalibrationResult result)
        {
            if (result == null)
                return "";

            double accuracy = result.AverageErrorDegree;

            if (accuracy < 0.5)
            {
                validResult = true;
                return "PERFECT";
            }
            if (accuracy < 0.7)
            {
                validResult = true;
                return "GOOD";
                //return "Calibration Quality: GOOD";
            }

            if (accuracy < 1)
            {
                validResult = true;
                return "MODERATE";
            }

            if (accuracy < 1.5)
            {
                validResult = false;
                return "POOR";
            }

            validResult = false;
            return "REDO";
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            GazeManager.Instance.Deactivate();
            Environment.Exit(0);
        }

        private void stop_Click(object sender, RoutedEventArgs e) //Is this necessary, now that we can deactivate after accepting calibration results?
        {
            GazeManager.Instance.Deactivate();
        }
    }
}
