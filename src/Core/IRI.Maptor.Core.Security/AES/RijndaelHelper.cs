using System;
using System.IO;
using System.Security.Cryptography;

namespace IRI.Maptor.Core.Security;

public static class RijndaelHelper
{
    public static string AesEncrypt(string value, byte[] key, byte[] iv)
    {
        using (RijndaelManaged rm = new RijndaelManaged())
        {
            using (var encryptor = rm.CreateEncryptor(key, iv))
            {
                var original = System.Text.Encoding.UTF8.GetBytes(value);

                return Convert.ToBase64String(Transform(original, encryptor));
            }
        }
    }

    public static string AesDecrypt(string base64String, byte[] key, byte[] iv)
    {
        using (RijndaelManaged rm = new RijndaelManaged())
        {
            using (var decryptor = rm.CreateDecryptor(key, iv))
            {
                var original = Convert.FromBase64String(base64String);

                return System.Text.Encoding.UTF8.GetString(Transform(original, decryptor));
            }
        }
    }


    #region Private Methods

    private static byte[] Transform(byte[] buffer, ICryptoTransform transform)
    {
        MemoryStream stream = new MemoryStream();

        using (CryptoStream cs = new CryptoStream(stream, transform, CryptoStreamMode.Write))
        {
            cs.Write(buffer, 0, buffer.Length);
        }

        return stream.ToArray();
    }

    #endregion


    #region Another Sample
    private static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
    {

        MemoryStream ms = new MemoryStream();

        Rijndael alg = Rijndael.Create();
        alg.Key = Key;

        alg.IV = IV;
        CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);

        cs.Write(clearData, 0, clearData.Length);
        cs.Close();
        byte[] encryptedData = ms.ToArray();
        return encryptedData;
    }


    public static string Encrypt(string Data, string Password, int Bits)
    {
        byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(Data);

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

            new byte[] { 0x00, 0x01, 0x02, 0x1C, 0x1D, 0x1E, 0x03, 0x04, 0x05, 0x0F, 0x20, 0x21, 0xAD, 0xAF, 0xA4 });

        if (Bits == 128)
        {
            byte[] encryptedData = Encrypt(clearBytes, pdb.GetBytes(16), pdb.GetBytes(16));
            return Convert.ToBase64String(encryptedData);
        }
        else if (Bits == 192)
        {
            byte[] encryptedData = Encrypt(clearBytes, pdb.GetBytes(24), pdb.GetBytes(16));
            return Convert.ToBase64String(encryptedData);
        }
        else if (Bits == 256)
        {
            byte[] encryptedData = Encrypt(clearBytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return Convert.ToBase64String(encryptedData);
        }
        else
        {
            return string.Concat(Bits);
        }
    }


    private static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
    {

        MemoryStream ms = new MemoryStream();
        Rijndael alg = Rijndael.Create();
        alg.Key = Key;
        alg.IV = IV;
        CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(cipherData, 0, cipherData.Length);
        cs.Close();
        byte[] decryptedData = ms.ToArray();
        return decryptedData;
    }


    public static string Decrypt(string Data, string Password, int Bits)
    {

        byte[] cipherBytes = Convert.FromBase64String(Data);

        PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

            new byte[] { 0x00, 0x01, 0x02, 0x1C, 0x1D, 0x1E, 0x03, 0x04, 0x05, 0x0F, 0x20, 0x21, 0xAD, 0xAF, 0xA4 });

        if (Bits == 128)
        {
            byte[] decryptedData = Decrypt(cipherBytes, pdb.GetBytes(16), pdb.GetBytes(16));
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }
        else if (Bits == 192)
        {
            byte[] decryptedData = Decrypt(cipherBytes, pdb.GetBytes(24), pdb.GetBytes(16));
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }
        else if (Bits == 256)
        {
            byte[] decryptedData = Decrypt(cipherBytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }
        else
        {
            return string.Concat(Bits);
        }

    }

    #endregion

    #region Another Sample 2


    //static byte[] EncryptAes(string plainText, byte[] Key, byte[] IV)
    //{
    //    byte[] encrypted;
    //    // Create a new AesManaged.    
    //    using (AesManaged aes = new AesManaged())
    //    {
    //        // Create encryptor    
    //        ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
    //        // Create MemoryStream    
    //        using (MemoryStream ms = new MemoryStream())
    //        {
    //            // Create crypto stream using the CryptoStream class. This class is the key to encryption    
    //            // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
    //            // to encrypt    
    //            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
    //            {
    //                // Create StreamWriter and write data to a stream    
    //                using (StreamWriter sw = new StreamWriter(cs))
    //                    sw.Write(plainText);
    //                encrypted = ms.ToArray();
    //            }
    //        }
    //    }
    //    // Return encrypted data    
    //    return encrypted;
    //}

    //static string DecryptAes(byte[] cipherText, byte[] Key, byte[] IV)
    //{
    //    string plaintext = null;
    //    // Create AesManaged    
    //    using (AesManaged aes = new AesManaged())
    //    {
    //        // Create a decryptor    
    //        ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
    //        // Create the streams used for decryption.    
    //        using (MemoryStream ms = new MemoryStream(cipherText))
    //        {
    //            // Create crypto stream    
    //            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
    //            {
    //                // Read crypto stream    
    //                using (StreamReader reader = new StreamReader(cs))
    //                    plaintext = reader.ReadToEnd();
    //            }
    //        }
    //    }
    //    return plaintext;
    //}

    #endregion


    #region Simpler Aes

    //public class SimplerAES
    //{
    //    private static byte[] _key = { 123, 217, 19, 11, 24, 26, 85, 45, 114, 184, 27, 162, 37, 112, 222, 209, 241, 24, 175, 144, 173, 53, 196, 29, 24, 26, 17, 218, 131, 236, 53, 209 };

    //    // a hardcoded IV should not be used for production AES-CBC code
    //    // IVs should be unpredictable per ciphertext
    //    private static byte[] _vector; //= __Replace_Me_({ 146, 64, 191, 111, 23, 3, 113, 119, 231, 121, 221, 112, 79, 32, 114, 156 });

    //    private ICryptoTransform encryptor, decryptor;
    //    private UTF8Encoding encoder;


    //    public SimplerAES()
    //    {
    //        RijndaelManaged rm = new RijndaelManaged();
    //        rm.GenerateIV();
    //        _vector = rm.IV;
    //        //_vector = secret;
    //        encryptor = rm.CreateEncryptor(_key, _vector);
    //        decryptor = rm.CreateDecryptor(_key, _vector);
    //        encoder = new UTF8Encoding();
    //    }

    //    public byte[] GetIV()
    //    {
    //        return _vector;
    //    }

    //    public string Encrypt(string unencrypted)
    //    {
    //        return Convert.ToBase64String(Encrypt(encoder.GetBytes(unencrypted)));
    //    }

    //    public string Decrypt(string encrypted)
    //    {
    //        return encoder.GetString(Decrypt(Convert.FromBase64String(encrypted)));
    //    }

    //    public byte[] Encrypt(byte[] buffer)
    //    {
    //        return Transform(buffer, encryptor);
    //    }

    //    public byte[] Decrypt(byte[] buffer)
    //    {
    //        return Transform(buffer, decryptor);
    //    }

    //    protected byte[] Transform(byte[] buffer, ICryptoTransform transform)
    //    {
    //        MemoryStream stream = new MemoryStream();
    //        using (CryptoStream cs = new CryptoStream(stream, transform, CryptoStreamMode.Write))
    //        {
    //            cs.Write(buffer, 0, buffer.Length);
    //        }
    //        return stream.ToArray();
    //    }
    //}
    #endregion
}
