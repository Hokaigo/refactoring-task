using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClassLibrary
{
    public class Hashing
    {
        private readonly byte[] key;
        private readonly byte[] iv;

        public Hashing(byte[] key, byte[] iv)
        {
            this.key = key;
            this.iv = iv;
        }

        public string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static (string Key, string IV) GenerateEncryptionKeyAndIV()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                string key = Convert.ToBase64String(aes.Key);
                string iv = Convert.ToBase64String(aes.IV);

                return (key, iv);
            }
        }
    }
}

