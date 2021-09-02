using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using PemUtils;
using System.Numerics;
using System.Windows.Forms;

namespace HostController
{
    class CryptoManager
    {
        private RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();
        private RSAParameters publicRSAKey;
        private RSAParameters privateRSAKey;
        public CryptoManager()
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
            
        }

        public RSAParameters getRSAParameters()
        {
            return privateRSAKey;
        }
        public RSAParameters getRSAParametersPublic()
        {
            return publicRSAKey;
        }
        public byte[] PrivateEncryption(byte[] data)
        {
            RSACryptoServiceProvider rsa = RSACSP;
            if (data == null)
                throw new ArgumentNullException("data");
            if (rsa.PublicOnly)
                throw new InvalidOperationException("Private key is not loaded");

            int maxDataLength = (rsa.KeySize / 8) - 6;
            if (data.Length > maxDataLength)
                throw new ArgumentOutOfRangeException("data", string.Format(
                    "Maximum data length for the current key size ({0} bits) is {1} bytes (current length: {2} bytes)",
                    rsa.KeySize, maxDataLength, data.Length));

            // Add 4 byte padding to the data, and convert to BigInteger struct
            BigInteger numData = GetBig(AddPadding(data));

            RSAParameters rsaParams = rsa.ExportParameters(true);
            BigInteger D = GetBig(rsaParams.D);
            BigInteger Modulus = GetBig(rsaParams.Modulus);
            BigInteger encData = BigInteger.ModPow(numData, D, Modulus);

            return encData.ToByteArray();
        }

        public byte[] PublicDecryption(byte[] cipherData)
        {
            RSACryptoServiceProvider rsa = RSACSP;
            if (cipherData == null)
                throw new ArgumentNullException("cipherData");

            BigInteger numEncData = new BigInteger(cipherData);

            RSAParameters rsaParams = rsa.ExportParameters(false);
            BigInteger Exponent = GetBig(rsaParams.Exponent);
            BigInteger Modulus = GetBig(rsaParams.Modulus);

            BigInteger decData = BigInteger.ModPow(numEncData, Exponent, Modulus);

            byte[] data = decData.ToByteArray();
            byte[] result = new byte[data.Length - 1];
            Array.Copy(data, result, result.Length);
            result = RemovePadding(result);

            Array.Reverse(result);
            return result;
        }

        private static BigInteger GetBig(byte[] data)
        {
            byte[] inArr = (byte[])data.Clone();
            Array.Reverse(inArr);  // Reverse the byte order
            byte[] final = new byte[inArr.Length + 1];  // Add an empty byte at the end, to simulate unsigned BigInteger
            Array.Copy(inArr, final, inArr.Length);

            return new BigInteger(final);
        }

        // Add 4 byte random padding, first bit *Always On*
        private static byte[] AddPadding(byte[] data)
        {
            Random rnd = new Random();
            byte[] paddings = new byte[4];
            rnd.NextBytes(paddings);
            paddings[0] = (byte)(paddings[0] | 128);

            byte[] results = new byte[data.Length + 4];

            Array.Copy(paddings, results, 4);
            Array.Copy(data, 0, results, 4, data.Length);
            return results;
        }

        private static byte[] RemovePadding(byte[] data)
        {
            byte[] results = new byte[data.Length - 4];
            Array.Copy(data, results, results.Length);
            return results;
        }

        public static void generateNewApplicationKeys()
        {
            RSACryptoServiceProvider tmpRSAPROV = new RSACryptoServiceProvider();
            DialogResult dr = MessageBox.Show("Public and/or Private Key Failed to load! (" + AppDomain.CurrentDomain.BaseDirectory + ") - Select Yes to regenerate keys. (WARNING You will lose access to all clients if you regenerate keys!)", "Error Loading Cryptographic Keys!", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if (dr == DialogResult.Yes)
            {
                tmpRSAPROV = new RSACryptoServiceProvider(4096);
                tmpRSAPROV.PersistKeyInCsp = false;
                using (var stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\publicKey.pzlcrt"))
                {
                    using (var writer = new PemWriter(stream))
                    {
                        writer.WritePublicKey(tmpRSAPROV.ExportParameters(false));
                    }
                }
                using (var stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\privateKey.pzlcrt"))
                {
                    using (var writer = new PemWriter(stream))
                    {
                        writer.WritePrivateKey(tmpRSAPROV.ExportParameters(true));
                    }
                }

            }
            else
            {
                MessageBox.Show("Application will now Exit.", "Exit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
        }
    }
}
