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

        private bool isCalibrated = false;
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
            // GazeManager.Instance.Activate(GazeManagerCore.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push,"localhost",6555);

            //REMOVE THIS
            ServerHandler.HandlerFacade.Observer.sendResponse("calibrate", "NOTIF");

            // Listen for changes in connection to server
            GazeManager.Instance.AddConnectionStateListener(this);
            port.Text = Convert.ToString(paraprocess.Program.Alpha._port);
            // Fetch current status
            OnConnectionStateChanged(GazeManager.Instance.IsActivated);

            // Add a fresh instance of the trackbox in case we reinitialize the client connection.
            TrackingStatusGrid.Children.Clear();
            TrackingStatusGrid.Children.Add(new TrackBoxStatus());

            UpdateState();
        }

        /*private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e == null)
                return;

            switch (e.Key)
            {
                // Start calibration on hitting "C"
                case Key.C:
                    ButtonCalibrateClicked(this, null);
                    break;
            }
        }
        */
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
                        isCalibrated = true;
                        UpdateState();

                        DialogResult result1 = System.Windows.Forms.MessageBox.Show("Calibration Result:" + port.ToString(),
                            "Click YES to accept the result. NO to discard.",
                            MessageBoxButtons.YesNo);
                        if (result1 == System.Windows.Forms.DialogResult.Yes && validResult)
                        {
                            //Send message that it server is calibrated
                            ServerHandler.HandlerFacade.Observer.sendResponse("calibrate", "NOTIF");
                            //ServerHandler.HandlerFacade.Observer.sendResponse(resultRating, "CALIB");
                        }
                        //MessageBox.Show(this, "Calibration success " + e.CalibrationResult.AverageErrorDegree);
                        break;
                    }

                case CalibrationRunnerResult.Abort:
                    MessageBox.Show(this, "The calibration was aborted. Reason: " + e.Message);
                    break;

                case CalibrationRunnerResult.Error:
                    MessageBox.Show(this, "An error occured during calibration. Reason: " + e.Message);
                    break;

                case CalibrationRunnerResult.Failure:
                    MessageBox.Show(this, "Calibration failed. Reason: " + e.Message);
                    break;

                case CalibrationRunnerResult.Unknown:
                    MessageBox.Show(this, "Calibration exited with unknown state. Reason: " + e.Message);
                    break;
            }
            // Show calibration results rating
        /*    if (e.Result == CalibrationRunnerResult.Success)
            {
                isCalibrated = true;
                UpdateState();

                DialogResult result1 = System.Windows.Forms.MessageBox.Show("Calibration Result:"+port.ToString(),
                    "Click YES to accept the result. NO to discard.",
                    MessageBoxButtons.YesNo);
                if(result1 == System.Windows.Forms.DialogResult.Yes && validResult)
                {
                    //Send message that it server is calibrated
                    ServerHandler.HandlerFacade.Observer.sendResponse("calibrate", "NOTIF");
                    //ServerHandler.HandlerFacade.Observer.sendResponse(resultRating, "CALIB");
                } 
            }
            else
                MessageBox.Show(this, "Calibration failed, please try again"); */
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
                return "Calibration Quality: PERFECT";
            }
            if (accuracy < 0.7)
            {
                validResult = true;
                return "Calibration Quality: GOOD";
            }

            if (accuracy < 1)
            {
                validResult = true;
                return "Calibration Quality: MODERATE";
            }

            if (accuracy < 1.5)
            {
                validResult = false;
                return "Calibration Quality: POOR";
            }

            validResult = false;
            return "Calibration Quality: REDO";
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            GazeManager.Instance.Deactivate();
            Environment.Exit(0);
        }
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //DO NOTHING.
        }
    }
}
