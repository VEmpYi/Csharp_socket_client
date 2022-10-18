using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace socket1_client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ~Form1()
        {
            socketSend.Disconnect(false);
            socketSend.Close();
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(txtPort.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                txtPort.Text = "0";
            }

            if (txtPort.Text == "")
            {
                txtPort.Text = "0";
            }

            if (Convert.ToInt32(txtPort.Text) < 0 || Convert.ToInt32(txtPort.Text) > 65535)
            {
                MessageBox.Show("Input Error! The value should range from 0 ~ 65535");
                txtPort.Text = "0";
            }
        }

        // create the socket responsible for communication
        Socket socketSend;
        Thread receive;

        bool connected = false;
        
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (connected)
                {
                    ShowMsg("The client already has a connected remote server!");
                }
                else
                {
                    socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress ip = IPAddress.Parse(txtIP.Text);
                    IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(txtPort.Text));
                    // Connect to the remote server application through IP address and port
                    socketSend.Connect(point);
                    connected = true;
                    ShowMsg("Connection succful!");
                    // create a new thread to receive the message from server
                    receive = new Thread(Receive);
                    receive.IsBackground = true;
                    receive.Start(socketSend);
                }
            }
            catch
            {
                ShowMsg("Connect failure!", 1);
            }
            
        }

        /// <summary>
        /// Send message to the log textbox
        /// </summary>
        /// <param name="str">Log message</param>
        /// <param name="level">log level default is INFO, ERROR is 1, MESG is 2</param>
        void ShowMsg(string str, int level = 0)
        {
            string time = DateTime.Now.ToString().Substring(11);
            string state;
            switch (level)
            {
                case 0:
                    state = "[INFO]";
                    break;
                case 1:
                    state = "[ERROR]";
                    break;
                case 2:
                    state = "[MESG]";
                    break;
                default:
                    state = "[NULL]";
                    break;
            }
            try
            {
                
                txtLog.AppendText(state + "[" + time + "]" + str + "\r\n");
            }
            catch
            {

            }
            
        }

        void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            try
            {
                while (true)
                {
                    if (connected)
                    {
                        byte[] buffer = new byte[1024 * 1024 * 2];
                        int r = socketSend.Receive(buffer);
                        if (r == 0)
                        {
                            ShowMsg(socketSend.RemoteEndPoint.ToString() + ": " + "Close!");
                            socketSend.Disconnect(false);
                            socketSend.Close();
                            break;
                        }
                        string str = Encoding.UTF8.GetString(buffer, 0, r);
                        ShowMsg(socketSend.RemoteEndPoint + ": " + str, 2);
                    }
                    

                }
            }
            catch(Exception e)
            {
                if(!(e is System.Threading.ThreadAbortException))
                ShowMsg("Connection error! Location: Receive", 1);
            }
        }

        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            try
            {
                string str = txtMsg.Text.Trim();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
                bool answer = MessageHeaders(msgType.text,ref buffer);
                socketSend.Send(buffer);
                ShowMsg("Send to " + socketSend.RemoteEndPoint + ": " + str, 2);
                txtMsg.Clear();
            }
            catch
            {
                ShowMsg("Message send failure!", 1);
            }
        }

        /// <summary>
        /// Adds a message type header to the message to be sent
        /// </summary>
        /// <param name="type">message type</param>
        /// <param name="buffer">the message to be sent</param>
        /// <returns>whether the action was successful</returns>
        private bool MessageHeaders(msgType type, ref byte[] buffer)
        {
            try
            {
                List<byte> list = new List<byte>();
                list.Add((byte)type);
                list.AddRange(buffer);
                buffer = list.ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        enum msgType
        {
            text,
            file,
            shake
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (connected)
                {
                    receive.Abort();
                    // socketSend.Dispose();
                    socketSend.Close();
                    connected = false;

                    ShowMsg("Connection close!");
                }
                else
                {
                    ShowMsg("No remote server to disconnect!");
                }
            }
            catch
            {
                ShowMsg("Disconnect failure", 1);
            }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //socketSend.Disconnect(false);
            //socketSend.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }
    }
}
