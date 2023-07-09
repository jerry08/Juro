using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Juro.Utils
{
    /// <summary>
    /// An Aes Encryptor/Decryptor
    /// </summary>
    internal static class AesHelper
    {
        public static string Encrypt(string plainText, string passphrase)
        {
            // generate salt
            byte[] key, iv;
            var salt = new byte[8];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(salt);
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            // encrypt bytes
            var encryptedBytes = EncryptStringToBytesAes(plainText, key, iv);
            // add salt as first 8 bytes
            var encryptedBytesWithSalt = new byte[salt.Length + encryptedBytes.Length + 8];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("Salted__"), 0, encryptedBytesWithSalt, 0, 8);
            Buffer.BlockCopy(salt, 0, encryptedBytesWithSalt, 8, salt.Length);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedBytesWithSalt, salt.Length + 8, encryptedBytes.Length);
            // base64 encode
            return Convert.ToBase64String(encryptedBytesWithSalt);
        }

        public static string Decrypt(string encrypted, string passphrase)
        {
            // base 64 decode
            var encryptedBytesWithSalt = Convert.FromBase64String(encrypted);
            // extract salt (first 8 bytes of encrypted)
            var salt = new byte[8];
            var encryptedBytes = new byte[encryptedBytesWithSalt.Length - salt.Length - 8];
            Buffer.BlockCopy(encryptedBytesWithSalt, 8, salt, 0, salt.Length);
            Buffer.BlockCopy(encryptedBytesWithSalt, salt.Length + 8, encryptedBytes, 0, encryptedBytes.Length);
            // get key and iv
            byte[] key, iv;
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            return DecryptStringFromBytesAes(encryptedBytes, key, iv);
        }

        private static void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
        {
            // generate key and iv
            var concatenatedHashes = new List<byte>(48);

            var password = Encoding.UTF8.GetBytes(passphrase);
            var currentHash = new byte[0];
            var md5 = MD5.Create();
            var enoughBytesForKey = false;
            // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
            while (!enoughBytesForKey)
            {
                var preHashLength = currentHash.Length + password.Length + salt.Length;
                var preHash = new byte[preHashLength];

                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
                Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

                currentHash = md5.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);

                if (concatenatedHashes.Count >= 48)
                {
                    enoughBytesForKey = true;
                }
            }

            key = new byte[32];
            iv = new byte[16];
            concatenatedHashes.CopyTo(0, key, 0, 32);
            concatenatedHashes.CopyTo(32, iv, 0, 16);

            md5.Clear();
            md5 = null;
        }

        private static byte[] EncryptStringToBytesAes(string plainText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (plainText is null || plainText.Length == 0)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            if (key is null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null || iv.Length == 0)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            // Declare the stream used to encrypt to an in memory
            // array of bytes.
            MemoryStream msEncrypt;

            // Declare the RijndaelManaged object
            // used to encrypt the data.
            RijndaelManaged aesAlg = default!;

            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged
                    { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

                // Create an encryptor to perform the stream transform.
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                //Write all data to the stream.
                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                swEncrypt.Close();
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return msEncrypt.ToArray();
        }

        private static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText is null || cipherText.Length == 0)
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            if (key is null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null || iv.Length == 0)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = default!;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged
                    { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

                // Create a decrytor to perform the stream transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                // Read the decrypted bytes from the decrypting stream
                // and place them in a string.
                plaintext = srDecrypt.ReadToEnd();
                srDecrypt.Close();
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            return plaintext;
        }
    }
}