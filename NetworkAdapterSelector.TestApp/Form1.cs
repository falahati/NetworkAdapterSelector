using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace NetworkAdapterSelector.TestApp
{
    public partial class Form1 : Form
    {
        private int _i = 1;
        private Socket _socket;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect("google.com", 80);
            MessageBox.Show("Connected");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Disconnect(false);
            MessageBox.Show("Disconnected");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var client = new WebClient {Proxy = null};
            var str = client.DownloadString("https://api.myip.com");
            client.Dispose();
            MessageBox.Show($"Downloaded: {str}");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var client = new WebClient();
            var str = client.DownloadString("https://api.myip.com");
            client.Dispose();
            MessageBox.Show($"Downloaded: {str}");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Text = (_i++).ToString();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    Thread.Sleep(500);
                    var client = new WebClient {Proxy = null};
                    var str = client.DownloadString("https://api.myip.com");
                    client.Dispose();
                    MessageBox.Show($"Downloaded: {str}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }).Start();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName, "");
        }
    }
}