using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

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

        /// <summary>
        /// Try to ensure the correct port number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        private Socket socketSend;

        private Thread receive;

        // Create a variable to indicate whether the listener has started
        private bool connected = false;

        /// <summary>
        /// Create a socket and start listening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    // Create a Socket to listen
                    socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    // Get the listening address and listening port from the text box
                    IPEndPoint point = new IPEndPoint(IPAddress.Parse(txtIP.Text), Convert.ToInt32(txtPort.Text));
                    // Connect to the remote server application through IP address and port
                    socketSend.Connect(point);
                    // Change the running status of the server
                    connected = true;
                    // Indicate the connection status
                    cBoxConnect.CheckState = CheckState.Checked;
                    ShowMsg("Connection successful!");
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
        private void ShowMsg(string str, int level = 0)
        {
            try
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
                txtLog.AppendText(state + "[" + time + "]" + str + "\r\n");
            }
            catch { }
        }

        /// <summary>
        /// Process the received message based on the message type
        /// </summary>
        /// <param name="obj">thread</param>
        private void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            try
            {
                while (true)
                {
                    if (connected)
                    {
                        byte[] buffer = new byte[1024 * 1024 * 10];
                        int r = socketSend.Receive(buffer);
                        // if r == 0, the connection may be closed
                        if (r == 0)
                        {
                            ShowMsg(socketSend.RemoteEndPoint.ToString() + ": " + "Close!");
                            socketSend.Close();
                            // Indicate the connection status
                            cBoxConnect.CheckState = CheckState.Unchecked;
                            connected = false;
                            break;
                        }
                        if (buffer[0] == (byte)msgType.text)
                        {
                            string str = Encoding.UTF8.GetString(buffer, 1, r - 1);
                            ShowMsg(socketSend.RemoteEndPoint + ": " + str, 2);
                        }
                        else if (buffer[0] == (byte)msgType.file)
                        {
                            // Get the length of file name to separate the name from the buffer
                            int fileNameLength = (int)buffer[1];
                            byte[] btFileName = buffer.Skip(2).Take(fileNameLength).ToArray();
                            string fileName = Encoding.UTF8.GetString(btFileName);
                            // Create a save fule dialog
                            SaveFileDialog sfd = new SaveFileDialog();
                            string path = @"C:\Users\" + Environment.UserName + @"\Desktop";
                            sfd.InitialDirectory = path;
                            sfd.Title = "Save";
                            sfd.Filter = "All Files|*.*";
                            sfd.FileName = fileName;
                            sfd.ShowDialog(this);
                            string newPath = sfd.FileName;
                           // Write file
                            using (FileStream fsWrite = new FileStream(newPath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                fsWrite.Write(buffer, 2 + fileNameLength, r - 2 - fileNameLength);
                            }
                            ShowMsg("File transfer completed!");
                        }
                        else if (buffer[0] == (byte)msgType.shake)
                        {
                        }
                        else
                        {
                            ShowMsg("Message type error!", 1);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!(e is System.Threading.ThreadAbortException))
                    ShowMsg("Connection error! Location: Receive", 1);
            }
        }

        /// <summary>
        /// Send message to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            try
            {
                // Remove useless content from message
                string str = txtMsg.Text.Trim();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
                // Add the message type to the text
                bool answer = MessageHeaders(msgType.text, ref buffer);
                if (!answer) return;
                // Send the message
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
                ShowMsg("Content processing failure!", 1);
                return false;
            }
        }

        /// <summary>
        /// define the message types
        /// </summary>
        private enum msgType
        {
            text,
            file,
            shake
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (connected)
                {
                    // Try to disconnect the thread
                    receive.Abort();
                    // Close the connection
                    socketSend.Close();
                    // Indicate the connection status
                    cBoxConnect.CheckState = CheckState.Unchecked;
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

        /// <summary>
        /// Disconnect before closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            socketSend.Close();
        }

        /// <summary>
        /// Cancel cross-thread operation warning
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }
    }
}