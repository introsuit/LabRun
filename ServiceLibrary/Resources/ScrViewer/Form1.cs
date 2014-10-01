using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScrS_viewer_
{
    public partial class Form1 : Form
    {
        public String host { get; set; }
        public Form1()
        {
            InitializeComponent();
            using (System.IO.StreamReader file = new System.IO.StreamReader(@"\\BSSFILES2\Dept\adm\lr-temp\rds-key.txt"))
            {
                this.host = file.ReadLine();
            }
            axRDPViewer1.Connect(this.host, "User1", "");

        }



        private void button1_Click(object sender, EventArgs e)
        {
            string Invitation = this.host;
            axRDPViewer1.Connect(Invitation, "User1", "");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            axRDPViewer1.Disconnect();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panel1.Height = Screen.PrimaryScreen.Bounds.Height - 100;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this.host);
        }
    }
}
