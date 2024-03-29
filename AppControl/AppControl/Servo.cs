﻿using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Pwm;

namespace AppControl
{
    /// <summary>
    /// Laggos, addig nem tokeletes
    /// </summary>
    class Servo : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        public readonly int PIN_NUMBER;
        public readonly int FREQUENCY;
        public readonly int SIGNAL_DURATION;
        public readonly double MIN_PULSE_WIDTH;
        public readonly double MIDDLE_PULSE_WIDTH;
        public readonly double MAX_PULSE_WIDTH;
        public readonly int MAX_ANGLE;

        PwmPin pin;
        PwmController controller;
        Timer t;

        private Object lockObject = new Object();
        #endregion

        #region Properties
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Boolean property. If true the servo will follow the value of the Desired_____ properties
        /// </summary>
        public bool AutoFollow
        {
            get { return autoFollow; }
            set { Set(ref autoFollow, value); }
        }
        private bool autoFollow = true;

        /// <summary>
        /// You can set the desired angle here. If you set, the desired pulse with will be calculated.
        /// </summary>
        public int DesiredAngle
        {
            get
            {
                return desiredAngle;
            }
            set
            {
                if (value < 0 || value > MAX_ANGLE)
                    throw new ArgumentException("The angle of the servo must be between 0 and MAX_ANGLE");

                if (value == 0)
                    desiredPulseWidth = MIN_PULSE_WIDTH;
                else
                    desiredPulseWidth = MIN_PULSE_WIDTH + (MAX_PULSE_WIDTH - MIN_PULSE_WIDTH) / ((double)MAX_ANGLE / value);

                RaisePropertyChanged(nameof(DesiredPulseWidth));

                Set(ref desiredAngle, value);

                if (AutoFollow)
                {
                    var percentage = desiredPulseWidth / (1000.0 / FREQUENCY);
                    pin.SetActiveDutyCyclePercentage(percentage);
                }
            }
        }
        private int desiredAngle;

        /// <summary>
        /// You can set the desired pusle width here. If you set, the desired angle will be calculated
        /// </summary>
        public double DesiredPulseWidth
        {
            get
            {
                return desiredPulseWidth;
            }
            set
            {
                if (value < MIN_PULSE_WIDTH || value > MAX_PULSE_WIDTH)
                    throw new ArgumentException("Pulsewidth is out of range");

                desiredAngle = (int)(((value - MIN_PULSE_WIDTH) / (MAX_PULSE_WIDTH - MIN_PULSE_WIDTH)) * MAX_ANGLE);

                RaisePropertyChanged(nameof(DesiredAngle));

                Set(ref desiredPulseWidth, value);

                if (AutoFollow)
                {
                    var percentage = value / (1000.0 / FREQUENCY);
                    pin.SetActiveDutyCyclePercentage(percentage);
                }
            }
        }
        private double desiredPulseWidth;
        #endregion

        /// <summary>
        /// Ctor where you can set the min pulse with (0 angle) and the max puse width (max angle) and other important things
        /// that you can find in the documentation.
        /// </summary>
        /// <param name="pinNumber"></param>
        /// <param name="frequency"></param>
        /// <param name="minPulseWidth"></param>
        /// <param name="maxPulseWidth"></param>
        /// <param name="maxAngle"></param>
        /// <param name="signalDuration"></param>
        public Servo(int pinNumber, int frequency = 50, double minPulseWidth = 1, double maxPulseWidth = 2, int maxAngle = 180, int signalDuration = 40)
        {
            this.PIN_NUMBER = pinNumber;
            this.FREQUENCY = frequency;
            this.MIN_PULSE_WIDTH = minPulseWidth;
            this.MAX_PULSE_WIDTH = maxPulseWidth;
            this.MAX_ANGLE = maxAngle;
            this.SIGNAL_DURATION = signalDuration;
            this.MIDDLE_PULSE_WIDTH = ((maxPulseWidth - minPulseWidth) / 2) + minPulseWidth;
        }

        /// <summary>
        /// Initialize the servo. 
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            if (!LightningProvider.IsLightningEnabled)
            {
                throw new Exception("Servo can only be used with Lihtning provider");
            }


            controller = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];

            pin = controller.OpenPin(PIN_NUMBER);
            controller.SetDesiredFrequency(FREQUENCY);

            pin.Start();

            DesiredPulseWidth = MIDDLE_PULSE_WIDTH;

            MoveServo();
        }

        /// <summary>
        /// MoveServo method moves the servo into the position that desiredPulseWidth field determine.
        /// </summary>
        public void MoveServo()
        {
            var percentage = desiredPulseWidth / (1000.0 / FREQUENCY);
            pin.SetActiveDutyCyclePercentage(percentage);
        }


        /// <summary>
        /// Free up our resources.
        /// </summary>
        public void Dispose()
        {
            pin.Stop();
            pin.Dispose();
            pin = null;

            t.Dispose();
            t = null;
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            // if unchanged return false
            if (Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            // if PropertyChanged not null call the Invoke method
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}