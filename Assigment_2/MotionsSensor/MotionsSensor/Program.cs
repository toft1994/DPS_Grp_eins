using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;
using Phidget22;
using uPLibrary.Networking.M2Mqtt;

namespace MotionsSensor
{
    class Program
    {
        private static int numOfSamples = 4000;
        private static int sampleingRate = 1000;
        private static bool samplingDone = false;
        private static int sampleCounter = 0;
        private static Complex32[] samples = new Complex32[numOfSamples];
        private static float[] res = new float[numOfSamples];
        private static double weight0 = 0;
        private static double weight1 = 0;
        private static bool weight_lifted_transmitted = false;
        private static bool weight_placed_transmitted = true;
        private static MqttClient client;
        private static VoltageInput MotionSensor;
        private static VoltageRatioInput weight_ch0;
        private static VoltageRatioInput weight_ch1;

        private static void MotionSensor_VoltageChange(object sender, Phidget22.Events.VoltageInputVoltageChangeEventArgs e)
        {
            if (sampleCounter < numOfSamples)
            {
                samples[sampleCounter] = new Complex32((float)(e.Voltage - 2.5), 0);
                sampleCounter++;
            }
            else
            {
                samplingDone = true;
            }
        }

        private static void Weight_ch0_VoltageChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
        {
            // Channel 0
            double m0 = 33037568.632; // Slope
            double b0 = 749.585; // Intercept
            weight0 = (m0 * e.VoltageRatio) + b0;
            //Console.WriteLine(e.VoltageRatio);
        }

        private static void Weight_ch1_VoltageChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
        {
            // Channel 3
            double m3 = 33459496.309; // Slope
            double b3 = 534.297; // Intercept
            weight1 = (m3 * e.VoltageRatio) + b3;
        }

        private static string GetActivity()
        {
            string ret = "No";

            double someActivity = res[0] + res[1] + res[2]*2;
            double alotActivity = res[3] + res[4] + res[5] + res[6] + res[7];

            if (someActivity > 0.1 && someActivity > alotActivity)
            {
                ret = "Some";
            }
            else if (alotActivity > 0.1 && alotActivity > someActivity)
            {
                ret = "Alot";
            }

            return ret;
        }

        private static void calculateFFT()
        {
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(samples);

            for (int i = 0; i < numOfSamples; i++)
            {
                res[i] = samples[i].Magnitude / (numOfSamples / 16);
            }
        }

        private static void OpenMotionSensor()
        {
            MotionSensor = new VoltageInput();
            MotionSensor.DeviceSerialNumber = 261154;
            MotionSensor.Channel = 1;
            MotionSensor.VoltageChange += MotionSensor_VoltageChange;

            MotionSensor.Open(Phidget.DefaultTimeout);
            MotionSensor.DataRate = sampleingRate;
        }

        private static void OpenWeightLoadCell()
        {
            weight_ch0 = new VoltageRatioInput();
            weight_ch1 = new VoltageRatioInput();

            weight_ch0.DeviceSerialNumber = 408235;
            weight_ch1.DeviceSerialNumber = 408235;
            weight_ch0.Channel = 0;
            weight_ch1.Channel = 3;
            weight_ch0.VoltageRatioChange += Weight_ch0_VoltageChange;
            weight_ch1.VoltageRatioChange += Weight_ch1_VoltageChange;

            weight_ch0.Open(Phidget.DefaultTimeout);
            weight_ch1.Open(Phidget.DefaultTimeout);
        }

        private static void ConnectMQTT()
        {
            client = new MqttClient("2fefc75adc594dab9da34a7efcbfc6df.s1.eu.hivemq.cloud", 8883, true, null, null, MqttSslProtocols.TLSv1_2);
            client.Connect("Phigets", "HiveMQJalle", "DankMayMays123");
        }

        static void Main(string[] args)
        {
            OpenMotionSensor();
            OpenWeightLoadCell();
            ConnectMQTT();

            while (client.IsConnected)
            {
                if (samplingDone)
                {
                    calculateFFT();
                    sampleCounter = 0;
                    var res = GetActivity();
                    client.Publish("MotionSensor", Encoding.UTF8.GetBytes(res));
                    Console.WriteLine(res);
                    samplingDone = false;
                }

                if (weight0 + weight1 < 50)
                {
                    if (!weight_lifted_transmitted)
                    {
                        client.Publish("WeightSensor", Encoding.UTF8.GetBytes("Off"));
                        Console.WriteLine("Pilleglas løftet!");
                        weight_placed_transmitted = false;
                        weight_lifted_transmitted = true;
                    }
                }
                else
                {
                    if (!weight_placed_transmitted)
                    {
                        client.Publish("WeightSensor", Encoding.UTF8.GetBytes("On"));
                        Console.WriteLine("Pilleglas sat!");
                        weight_lifted_transmitted = false;
                        weight_placed_transmitted = true;
                    }
                }
            }
        }
    }
}
