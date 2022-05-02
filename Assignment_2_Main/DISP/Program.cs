using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace DISP
{
    partial class Program
    {
        static MqttClient client;
        static bool pillsTaken = false;
        static bool pillsPutBack = false;
        static int numberOfAlotMovements = 0;
        static string previousMovement = "tmp";
        const string zigbeeLedAddress = "zigbee2mqtt/0x680ae2fffec0cbba";
        static bool GoOutOfLoop = false;

        static void Main(string[] args)
        {
            #region Connection

            client = new MqttClient("2fefc75adc594dab9da34a7efcbfc6df.s1.eu.hivemq.cloud", 8883, true, null, null, MqttSslProtocols.TLSv1_2);
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Connect("Morten_Dalsgaard", "HiveMQJalle", "DankMayMays123");
            Console.WriteLine("Client Connection status: " + client.IsConnected);
            #endregion

            #region Subscriptions
            client.Subscribe(new string[] { "zigbee2mqtt/0x680ae2fffec0cbbb", "MotionSensor", "WeightSensor" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            #endregion

            //Mp3 player
            SoundPlayer player = new SoundPlayer();

            //Start by turning off LED
            var led = new LED();
            led.state = "OFF";
            client.Publish(zigbeeLedAddress + "/set", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(led)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);


            var led1 = new LED();
            led1.brightness = 254;
            led1.state = "ON";
            var col1 = new Color();
            col1.r = 200;
            col1.b = 0;
            col1.g = 0;
            led1.color = col1;

            var firstStopWatch = new Stopwatch();

            while (true)
            {
                if (numberOfAlotMovements == 2)
                {
                    player.SoundLocation = "C:\\Users\\mongl\\source\\repos\\DISP\\DISP\\GodmorgenPIllerWav.wav";
                    player.Load();
                    player.Play();

                    firstStopWatch.Start();
                    while (true)
                    {
                        //Person has awoken - sets the LED to blink to indicate the person has to take his medicine.
                        client.Publish(zigbeeLedAddress + "/set", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(led1)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);


                        Thread.Sleep(2500);

                        col1.r = 0;
                        col1.g = 200;
                        client.Publish(zigbeeLedAddress + "/set", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(led1)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);


                        Thread.Sleep(2500);
                        col1.r = 200;
                        col1.g = 0;

                        if (firstStopWatch.Elapsed.Seconds >= 20 && GoOutOfLoop == false)
                        {
                            player.SoundLocation = "C:\\Users\\mongl\\source\\repos\\DISP\\DISP\\SpisDinePillerWav.wav";
                            player.Load();
                            player.Play();
                        }

                        //If he has lifted the pills the program will break out of the blinking mode and set the color to a default white color.
                        if (pillsTaken)
                        {
                            firstStopWatch.Stop();
                            
                            break;
                        }
                    }

                    var led2 = new LED();
                    led2.brightness = 254;
                    led2.state = "ON";
                    var col2 = new Color();
                    col2.r = 200;
                    col2.b = 200;
                    col2.g = 200;
                    led2.color = col2;

                    client.Publish(zigbeeLedAddress + "/set", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(led2)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                    //Checks if the person do not put the pills back.

                    firstStopWatch.Reset();
                    firstStopWatch.Start();

                    while (true)
                    {
                        if (firstStopWatch.Elapsed.Seconds >= 10)
                        {
                            player.SoundLocation = "C:\\Users\\mongl\\source\\repos\\DISP\\DISP\\PillerpåpladsikkespisientimeWav.wav";
                            player.Load();
                            player.Play();
                            firstStopWatch.Restart();
                        }

                        //If the pills are put back we stop the timing.
                        if (pillsPutBack)
                        {
                            break;
                        }

                    }

                    firstStopWatch.Stop();

                    //Turn off the alarm
                    client.Publish(zigbeeLedAddress + "/set", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(led)), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    
                    
                    Console.ReadLine();
                }

            }

        }
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            switch (e.Topic)
            {
                case zigbeeLedAddress:
                    var res = Encoding.UTF8.GetString(e.Message);
                    Console.WriteLine($"LED-STRIP MESSAGE: {res}");
                    break;
                case "MotionSensor":
                    var res2 = Encoding.UTF8.GetString(e.Message);
                    Console.WriteLine($"MOTIONSENSOR MESSAGE: {res2}");
                    OnMotionEvent(res2);
                    break;
                case "WeightSensor":
                    var res3 = Encoding.UTF8.GetString(e.Message);
                    Console.WriteLine($"WEIGHTSENSOR MESSAGE: {res3}");
                    OnWeightEvent(res3);
                    break;
                default:
                    Console.WriteLine("Reached default in switch statement");
                    break;
            }
        }

        static void OnMotionEvent(string message)
        {

            if (!pillsTaken)
            {
                switch (message)
                {
                    case "Alot":

                        if (previousMovement == "Alot")
                        {
                            numberOfAlotMovements++;
                        }
                        else
                        {
                            numberOfAlotMovements = 0;
                        }

                        previousMovement = message;

                        break;
                    case "No":
                        previousMovement = message;
                        break;
                    case "Some":
                        previousMovement = message;
                        break;
                    default:
                        break;
                }
            }
        }

        static void OnWeightEvent(string message)
        {

            if (message == "On")
            {
                pillsPutBack = true;
            }

            if (message == "Off")
            {
                pillsTaken = true;
                GoOutOfLoop = true;
            }
        }
    }
}
