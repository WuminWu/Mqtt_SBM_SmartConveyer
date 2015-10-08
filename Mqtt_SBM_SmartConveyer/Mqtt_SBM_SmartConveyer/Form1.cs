/*
 * 2015/10/06
 * MQTT client for Cell Controller
 * 
 * by Wumin
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net.Sockets;    //use this namespace for sockets
using System.Net;            //for ip addressing
using System.IO;             //for streaming io
using System.Threading;      //for running threads
using System.Reflection;
using System.Globalization;
using System.Security.Cryptography;

// Add robot and Mqtt library
using IACT_RobotLibrary;
using uPLibrary.Networking.M2Mqtt;

namespace Mqtt_SBM_SmartConveyer
{
    public partial class Form1 : Form
    {
        private bool InitControllerResult = false, InitControllerResult2 = false, InitRobotResult, InitMessagePort;
        private string pwd = "1234";
        private string robotIP = "192.168.0.3";
        private int robotPort = 5000;

        // variable of Mqtt
        const string ipAddress = "127.0.0.1";
        const string TOPIC_command = "Demo/Robot/1/Command";
        const string TOPIC_result = "Demo/Robot/1/Result";
        const string PAYLOAD_move12 = "mov 1,2";
        const string PAYLOAD_move21 = "mov 2,1";
        const string PAYLOAD_respOK = "OK";
        const string PAYLOAD_respNG = "NG";

        private Thread MsgThread, MsgThread2, MsgThread3;
        public TcpClient tcpClient = new TcpClient();
        MqttClient client = new MqttClient(IPAddress.Parse(ipAddress));
        public static IACT_RobotLibrary.IACT_RobotLibrary robot = new IACT_RobotLibrary.IACT_RobotLibrary();

        public Form1()
        {
            InitializeComponent();
        }

        // Mqtt Protocol Function
        public void creatMqttClient(string ip)
        {
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;     // Define what function to call when a message arrives
            client.MqttMsgSubscribed += client_MqttMsgSubscribed;               // Define what function to call when a subscription is acknowledged
            client.MqttMsgPublished += client_MqttMsgPublished;                 // Define what function to call when a message is published

            string clientID = Guid.NewGuid().ToString();
            byte connection = client.Connect(clientID);

            // Wumin : if "RetainedOne" subscribe here and you will get msg twice
            ushort subscribe = client.Subscribe(new string[] { TOPIC_command, TOPIC_result }, new byte[] { 0, 0 });
            Console.WriteLine("subscribe = " + subscribe);
        }

        private static void client_MqttMsgPublished(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishedEventArgs e)
        {
            Console.Write("Message " + e.MessageId + " has been sent.\n");
        }

        // Handle subscription acknowledgements
        private static void client_MqttMsgSubscribed(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgSubscribedEventArgs e)
        {
            Console.WriteLine("Subscribed!");
        }

        // Handle incoming messages
        private static void client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            string publishReceived = Encoding.UTF8.GetString(e.Message);
            Console.WriteLine("Message received: "+publishReceived);

            switch (publishReceived)
            {
                case PAYLOAD_move12 :
                    action_movAtoB();
                    break;

                case PAYLOAD_move21 :
                    action_movBtoA();
                    break;

                default:
                    //Console.WriteLine("publishReceived has no event");
                    break;
            }
        }

        private static void action_movAtoB()
        {
            if (robot != null && robot.IsRobotConnected() == true)
            {
                string response;
                // Select Script 0
                response = robot.SendCommand("$Start,0");
                Thread.Sleep(100);
                if (response == null)
                {
                    MessageBox.Show("No Programing File");
                    return;
                }
            }
            else
                MessageBox.Show("Robot Connect Fail");
        }

        private static void action_movBtoA()
        {
            if (robot != null && robot.IsRobotConnected() == true)
            {
                string response;
                // Select Script 1
                response = robot.SendCommand("$Start,1");
                Thread.Sleep(100);
                if (response == null)
                {
                    MessageBox.Show("No Programing File");
                    return;
                }
            }
            else
                MessageBox.Show("Robot Connect Fail");
        }

        private void button_robotConnect(object sender, EventArgs e)
        {
            // Connect with Robot Controller
            InitRobotResult = robot.InitRobotOnly(robotIP, robotPort);

            if (InitRobotResult == true)
            {
                MessageBox.Show("Robot Connect Successfully");
            }
            else
                MessageBox.Show("Robot Connect Fail");
        }

        private void button_stop(object sender, EventArgs e)
        {
            string response = robot.StopRunRobot();
            if (response == null)
                MessageBox.Show("Stop Robot Fail");
        }

        private void button_login(object sender, EventArgs e)
        {
            string response = robot.EpsonLoginRobot(pwd);
            if (response == null)
                MessageBox.Show("Login Fail");
            if (response.Equals("#Login,0"))
                MessageBox.Show("Login Success");
        }

        private void button_logout(object sender, EventArgs e)
        {
            string response = robot.EpsonLogoutRobot();
            if (response == null)
                MessageBox.Show("Logout Fail");
        }

        private void button_motorOn(object sender, EventArgs e)
        {
            string response = robot.ServerOn(true);
            if (response == null)
                MessageBox.Show("MotorOn Fail");
        }

        private void button_motorOff(object sender, EventArgs e)
        {
            string response = robot.ServerOn(false);
            if (response == null)
                MessageBox.Show("MotorOff Fail");
        }

        private void button_mqttConnect(object sender, EventArgs e)
        {
            creatMqttClient(ipAddress);
        }

        private void button_movAtoB(object sender, EventArgs e)
        {
            //If you are using QoS Level 1 or 2 to publish a message on a specified topic, 
            //you can also register to MqttMsgPublished event that will be raised 
            //when the message will be delivered (exactly once) to all subscribers on the topic
            ushort publishAtoB = client.Publish(TOPIC_command, Encoding.UTF8.GetBytes(PAYLOAD_move12));
        }

        private void button_movBtoA(object sender, EventArgs e)
        {
            ushort publishBtoA = client.Publish(TOPIC_result, Encoding.UTF8.GetBytes(PAYLOAD_move21));
        }

        private void button_OK(object sender, EventArgs e)
        {
            ushort publishOK = client.Publish(TOPIC_result, Encoding.UTF8.GetBytes(PAYLOAD_respOK));
        }

        private void button_NG(object sender, EventArgs e)
        {
            ushort publishNG = client.Publish(TOPIC_result, Encoding.UTF8.GetBytes(PAYLOAD_respNG));
        }
    }
}
