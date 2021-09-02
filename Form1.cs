using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using PemUtils;


namespace HostController
{
    public partial class Form1 : Form
    {
        static List<Client> rootClients = new List<Client>();
        RSAParameters publicRSAKey;
        RSAParameters privateRSAKey;
        RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();


        Server server = new Server(rootClients);

        public Form1()
        {
            InitializeComponent();
            RSACSP.PersistKeyInCsp = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            server.setupServer();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This Program was Developed by Prozel Cloud Solutions for Remote Endpoint Monitoring.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                using (var stream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\publicKey.pzlcrt"))
                {
                    using (var reader = new PemReader(stream))
                    {
                        publicRSAKey = reader.ReadRsaKey();
                    }
                }
                using (var stream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\privateKey.pzlcrt"))
                {
                    using (var reader = new PemReader(stream))
                    {
                        privateRSAKey = reader.ReadRsaKey();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                generateNewApplicationKeys();
                using (var stream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\publicKey.pzlcrt"))
                {
                    using (var reader = new PemReader(stream))
                    {
                        publicRSAKey = reader.ReadRsaKey();
                    }
                }
                using (var stream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "\\privateKey.pzlcrt"))
                {
                    using (var reader = new PemReader(stream))
                    {
                        privateRSAKey = reader.ReadRsaKey();
                    }
                }
            }
            RSACSP.ImportParameters(privateRSAKey);
        }

        private void generateNewApplicationKeys()
        {
            DialogResult dr = MessageBox.Show("Public and/or Private Key Failed to load! (" + AppDomain.CurrentDomain.BaseDirectory + ") - Select Yes to regenerate keys. (WARNING You will lose access to all clients if you regenerate keys!)", "Error Loading Cryptographic Keys!", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if(dr == DialogResult.Yes)
            {
                RSACSP = new RSACryptoServiceProvider(4096);
                RSACSP.PersistKeyInCsp = false;
                using (var stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\publicKey.pzlcrt"))
                {
                    using (var writer = new PemWriter(stream))
                    {
                        writer.WritePublicKey(RSACSP.ExportParameters(false));
                    }
                }
               using (var stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\privateKey.pzlcrt"))
                {
                    using (var writer = new PemWriter(stream))
                    {
                        writer.WritePrivateKey(RSACSP.ExportParameters(true));
                    }
                }
                
            }
            else
            {
                MessageBox.Show("Application will now Exit.", "Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
        }

        private void clientHeartbeat_Tick(object sender, EventArgs e)
        {
            server.heartbeat();
        }

        private void displayKeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            displayCurrentKeys dispKeys = new displayCurrentKeys();
            dispKeys.Show();
        }
    }
}
