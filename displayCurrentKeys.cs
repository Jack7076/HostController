using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HostController
{
    public partial class displayCurrentKeys : Form
    {
        public displayCurrentKeys()
        {
            InitializeComponent();
        }

        private void displayCurrentKeys_Load(object sender, EventArgs e)
        {
            textBox1.Text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\publicKey.pzlcrt");
            textBox2.Text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\privateKey.pzlcrt");
        }
    }
}
