using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BadmintonCourtAutoBooker
{
    public static class EncryptManager
    {
        private const int KeySize = 128;
        private const int DerivationIterations = 1000;

        public static string Encrypt(this string oriText, string password)
        {
            if (string.IsNullOrEmpty(oriText) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            byte[] saltStringBytes = Generate128BitsOfRandomEntropy();
            byte[] ivStringBytes = Generate128BitsOfRandomEntropy();
            byte[] oriTextBytes = Encoding.UTF8.GetBytes(oriText);
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = rfc2898DeriveBytes.GetBytes(KeySize / 8);
                using (var rijndaelManaged = new RijndaelManaged())
                {
                    rijndaelManaged.BlockSize = 128;
                    rijndaelManaged.Mode = CipherMode.CBC;
                    rijndaelManaged.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(oriTextBytes, 0, oriTextBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(this string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(KeySize / 8).ToArray();
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(KeySize / 8).Take(KeySize / 8).ToArray();
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(KeySize / 8 * 2).Take(cipherTextBytesWithSaltAndIv.Length - (KeySize / 8 * 2)).ToArray();
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = rfc2898DeriveBytes.GetBytes(KeySize / 8);
                using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
                {
                    rijndaelManaged.BlockSize = 128;
                    rijndaelManaged.Mode = CipherMode.CBC;
                    rijndaelManaged.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate128BitsOfRandomEntropy()
        {
            byte[] randomBytes = new byte[16];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}