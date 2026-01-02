using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

public static class EncryptionUtility  {
    
    private static readonly string encryptionKey = "9f3c7a2e6b4d8a01c5f279e4b6d0a83f4c1e92ab57d068f3a4b8cde1290f7a6b";

    public static string EncryptString(string plainText)
    {
        byte[] key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0,32));
        using(Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.GenerateIV();
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new MemoryStream())
            {
                msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                using(var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using(var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public static string DecryptString(string cipherText)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherText);
        byte[] iv = new byte[16];
        byte[] cipher = new byte[fullCipher.Length - 16];
        
        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, 16, cipher, 0, cipher.Length);

        byte[] key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0,32));
        using(Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using(var msDecrypt = new MemoryStream(cipher))
            {
                using(var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using(var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}
