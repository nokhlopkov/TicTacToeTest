using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace TicTacToeTest
{
    public partial class ConnectToGame : Form
    {
        public static MainWindow Caller;

        public ConnectToGame(MainWindow spawner)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            Caller = spawner;
        }

        private void ConnectToGame_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IPAddress openIP = null;
            IPAddress.TryParse(this.IPField.Text, out openIP);
            int openPort = Convert.ToInt32(this.PortField.Text);

            if (openIP != null)
            {
                Caller.CurrentGame.client.Connect(openIP, openPort);

                if (Caller.CurrentGame.client.Connected)
                {
                    MessageBox.Show($"Connected to {openIP.ToString()}");
                    this.Dispose();
                    Caller.CurrentGame.StartGame();
                }
                else
                    MessageBox.Show($"Failed to connect to {openIP.ToString()}");
            }
            else
                MessageBox.Show("Invalid IP address");

            this.Dispose();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
