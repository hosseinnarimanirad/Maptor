using System;
using System.Security.Cryptography;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Security;


/// <summary>
/// AsymmetricCryptography > RSA
/// </summary>
public static class CryptoRSAHelper
{
    private static byte[] RSA_OID = { 0x30, 0xD, 0x6, 0x9, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0xD, 0x1, 0x1, 0x1, 0x5, 0x0 }; // Object ID for RSA

    // Corresponding ASN identification bytes
    const byte INTEGER = 0x2;
    const byte SEQUENCE = 0x30;
    const byte BIT_STRING = 0x3;
    const byte OCTET_STRING = 0x4;

    const int rsaKeySize = 2048;
     

    #region Encrypt / Decrypt

    /// <summary>
    /// Returns encrypted value in base64string
    /// </summary>
    /// <param name="original"></param>
    /// <param name="base46PublicKey"></param>
    /// <returns>encrypted value in base64string</returns>
    //public static string RsaEncrypt(byte[] original, string base46PublicKey)
    //{
    //    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(rsaKeySize))
    //    {
    //        try
    //        {
    //            Security.CryptoRSAHelper.FromXmlString(rsa, base46PublicKey.Base64ToNormalString());
    //            //rsa.FromXmlString(base46PublicKey.Base64ToNormalString());

    //            return Convert.ToBase64String(rsa.Encrypt(original, true));
    //        }
    //        catch (Exception)
    //        {

    //            throw;
    //        }
    //        finally
    //        {
    //            rsa.PersistKeyInCsp = false;
    //        }
    //    }
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="simpleString"></param>
    /// <param name="base46PublicKey"></param>
    /// <returns>encrypted value in base64string</returns>
    //public static string RsaEncrypt(string simpleString, string base46PublicKey)
    //{
    //    var originalByte = Encoding.UTF8.GetBytes(simpleString);

    //    return RsaEncrypt(originalByte, base46PublicKey);
    //}


    //public static string RsaDecrypt(byte[] encoded, string base64PrivateKey)
    //{
    //    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(rsaKeySize))
    //    {
    //        try
    //        {
    //            Security.CryptoRSAHelper.FromXmlString(rsa, base64PrivateKey.Base64ToNormalString());
    //            //rsa.FromXmlString(base64PrivateKey.Base64ToNormalString());

    //            return Encoding.UTF8.GetString(rsa.Decrypt(encoded, true));
    //        }
    //        catch (Exception)
    //        {
    //            throw;
    //        }
    //        finally
    //        {
    //            rsa.PersistKeyInCsp = false;
    //        }
    //    }
    //}

    //public static string RsaDecrypt(string base64EncodedString, string base64PrivateKey)
    //{
    //    var encodedBytes = Convert.FromBase64String(base64EncodedString);

    //    return RsaDecrypt(encodedBytes, base64PrivateKey);
    //}


    public static void test()
    {
        var csp = new RSACryptoServiceProvider(2048);

        //how to get the private key
        var privKey = csp.ExportParameters(true);

        //and the public key ...
        var pubKey = csp.ExportParameters(false);

        //converting the public key into a string representation
        string pubKeyString;
        {
            //we need some buffer
            var sw = new System.IO.StringWriter();
            //we need a serializer
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //serialize the key into the stream
            xs.Serialize(sw, pubKey);
            //get the string from the stream
            pubKeyString = sw.ToString();
        }

        //converting it back
        {
            //get a stream from the string
            var sr = new System.IO.StringReader(pubKeyString);
            //we need a deserializer
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //get the object back from the stream
            pubKey = (RSAParameters)xs.Deserialize(sr);
        }

        //conversion for the private key is no black magic either ... omitted

        //we have a public key ... let's get a new csp and load that key
        csp = new RSACryptoServiceProvider();
        csp.ImportParameters(pubKey);

        //we need some data to encrypt
        var plainTextData = "foobar";

        //for encryption, always handle bytes...
        var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTextData);

        //apply pkcs#1.5 padding and encrypt our data 
        var bytesCipherText = csp.Encrypt(bytesPlainTextData, false);

        //we might want a string representation of our cipher text... base64 will do
        var cipherText = Convert.ToBase64String(bytesCipherText);


        /*
         * some transmission / storage / retrieval
         * 
         * and we want to decrypt our cipherText
         */

        //first, get our bytes back from the base64 string ...
        bytesCipherText = Convert.FromBase64String(cipherText);

        //we want to decrypt, therefore we need a csp and load our private key
        csp = new RSACryptoServiceProvider();
        csp.ImportParameters(privKey);

        //decrypt and strip pkcs#1.5 padding
        bytesPlainTextData = csp.Decrypt(bytesCipherText, false);

        //get our original plainText back...
        plainTextData = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
    }




    //static public byte[] RsaEncrypt(byte[] value, RSAParameters rsaKey, bool doOAEPPadding = true)
    //{
    //    try
    //    {
    //        byte[] encryptedData;

    //        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
    //        {
    //            rsa.ImportParameters(rsaKey);

    //            encryptedData = rsa.Encrypt(value, doOAEPPadding);
    //        }

    //        return encryptedData;
    //    }
    //    catch (CryptographicException e)
    //    {
    //        return null;
    //    }
    //}

    //static public byte[] RsaDecrypt(byte[] value, RSAParameters rsaKey, bool doOAEPPadding)
    //{
    //    try
    //    {
    //        byte[] decryptedData;

    //        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
    //        {
    //            rsa.ImportParameters(rsaKey);

    //            decryptedData = rsa.Decrypt(value, doOAEPPadding);
    //        }

    //        return decryptedData;
    //    }
    //    catch (CryptographicException e)
    //    {
    //        return null;
    //    }
    //}

    #endregion

    #region Encrypt / Decrypt 

    /// <summary>
    /// Returns encrypted value in base64string
    /// </summary>
    /// <param name="original">Data to encrypt</param>
    /// <param name="base64PublicKey">Public key in base64 encoded XML format</param>
    /// <returns>Encrypted value in base64string</returns>
    public static string RsaEncrypt(byte[] original, string base64PublicKey)
    {
        using (RSA rsa = RSA.Create(rsaKeySize))
        {
            try
            {
                string publicKeyXml = base64PublicKey.Base64ToNormalString();
                FromXmlString(rsa, publicKeyXml);

                byte[] encryptedData = rsa.Encrypt(original, RSAEncryptionPadding.OaepSHA1);
                return Convert.ToBase64String(encryptedData);
            }
            finally
            {
                //rsa.PersistKeyInCsp = false;
            }
        }
    }

    /// <summary>
    /// Encrypts a string using RSA
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="base64PublicKey">Public key in base64 encoded XML format</param>
    /// <returns>Encrypted value in base64string</returns>
    public static string RsaEncrypt(string plainText, string base64PublicKey)
    {
        byte[] originalBytes = Encoding.UTF8.GetBytes(plainText);
        return RsaEncrypt(originalBytes, base64PublicKey);
    }

    /// <summary>
    /// Decrypts data using RSA
    /// </summary>
    /// <param name="encoded">Encrypted data</param>
    /// <param name="base64PrivateKey">Private key in base64 encoded XML format</param>
    /// <returns>Decrypted string</returns>
    public static string RsaDecrypt(byte[] encoded, string base64PrivateKey)
    {
        using (RSA rsa = RSA.Create(rsaKeySize))
        {
            try
            {
                string privateKeyXml = base64PrivateKey.Base64ToNormalString();
                FromXmlString(rsa, privateKeyXml);

                byte[] decryptedData = rsa.Decrypt(encoded, RSAEncryptionPadding.OaepSHA1);
                return Encoding.UTF8.GetString(decryptedData);
            }
            finally
            {
                //rsa.PersistKeyInCsp = false;
            }
        }
    }

    /// <summary>
    /// Decrypts a base64 encoded string using RSA
    /// </summary>
    /// <param name="base64EncodedString">Base64 encoded encrypted data</param>
    /// <param name="base64PrivateKey">Private key in base64 encoded XML format</param>
    /// <returns>Decrypted string</returns>
    public static string RsaDecrypt(string base64EncodedString, string base64PrivateKey)
    {
        byte[] encodedBytes = Convert.FromBase64String(base64EncodedString);

        return RsaDecrypt(encodedBytes, base64PrivateKey);
    }

    #endregion

    #region XML Representation of the Public/Private Keys

    // 1400.02.07
    // initialize RSA with a private or public key in xml format
    public static void FromXmlString(RSA rsa, string xmlString)
    {
        RSAParameters parameters = new RSAParameters();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
        {
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                }
            }
        }
        else
        {
            throw new Exception("Invalid XML RSA key.");
        }

        rsa.ImportParameters(parameters);
    }


    // 1400.02.07
    // get the xml format of the RSA private or public key
    public static string ToXmlString(RSA rsa, bool includePrivateParameters)
    {
        RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

        return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
              parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
              parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
              parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
              parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
              parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
              parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
              parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
              parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
    }

    // The default 1024-bit key is now considered insecure
    public static RsaKeys GenerateKeysInXml(int keySize = 2048)
    {
        // In .NET Core/.NET 5+, RSACryptoServiceProvider is obsolete
        //using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
        using (RSA rsa = RSA.Create(keySize))
        {
            try
            {
                return new RsaKeys(ToXmlString(rsa, true), ToXmlString(rsa, false));
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //rsa.PersistKeyInCsp = false;
            }
        }
    }

    public static void GenerateKeysInPem(out string base64PrivateKey, out string base64PublicKey, int keySize = 2048)
    {
        using (RSA rsa = RSA.Create(keySize))
        {
            // Export private key in PKCS#8 format (PEM)
            byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
            base64PrivateKey = FormatAsPem(
                "PRIVATE KEY",
                Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks)
            );

            // Export public key in X.509 SubjectPublicKeyInfo format (PEM)
            byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            base64PublicKey = FormatAsPem(
                "PUBLIC KEY",
                Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks)
            );
        }
    }

    private static string FormatAsPem(string keyType, string base64Content)
    {
        return $"-----BEGIN {keyType}-----\n{base64Content}\n-----END {keyType}-----\n";
    }
     

    #endregion


    #region Binary Representation of the Public/Private Keys 

    public static string GetPublicKeyInBinary(RSAParameters param)
    {
        List<byte> arrBinaryPublicKey = new List<byte>();

        arrBinaryPublicKey.InsertRange(0, param.Exponent);
        arrBinaryPublicKey.Insert(0, (byte)arrBinaryPublicKey.Count);
        arrBinaryPublicKey.Insert(0, INTEGER);

        arrBinaryPublicKey.InsertRange(0, param.Modulus);
        AppendLength(ref arrBinaryPublicKey, param.Modulus.Length);
        arrBinaryPublicKey.Insert(0, INTEGER);

        AppendLength(ref arrBinaryPublicKey, arrBinaryPublicKey.Count);
        arrBinaryPublicKey.Insert(0, SEQUENCE);

        arrBinaryPublicKey.Insert(0, 0x0); // Add NULL value

        AppendLength(ref arrBinaryPublicKey, arrBinaryPublicKey.Count);

        arrBinaryPublicKey.Insert(0, BIT_STRING);
        arrBinaryPublicKey.InsertRange(0, RSA_OID);

        AppendLength(ref arrBinaryPublicKey, arrBinaryPublicKey.Count);

        arrBinaryPublicKey.Insert(0, SEQUENCE);

        return System.Convert.ToBase64String(arrBinaryPublicKey.ToArray());
    }

    public static string GetPrivateKeyInBinary(RSAParameters param)
    {
        List<byte> arrBinaryPrivateKey = new List<byte>();

        arrBinaryPrivateKey.InsertRange(0, param.InverseQ);
        AppendLength(ref arrBinaryPrivateKey, param.InverseQ.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.DQ);
        AppendLength(ref arrBinaryPrivateKey, param.DQ.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.DP);
        AppendLength(ref arrBinaryPrivateKey, param.DP.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.Q);
        AppendLength(ref arrBinaryPrivateKey, param.Q.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.P);
        AppendLength(ref arrBinaryPrivateKey, param.P.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.D);
        AppendLength(ref arrBinaryPrivateKey, param.D.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.Exponent);
        AppendLength(ref arrBinaryPrivateKey, param.Exponent.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.InsertRange(0, param.Modulus);
        AppendLength(ref arrBinaryPrivateKey, param.Modulus.Length);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        arrBinaryPrivateKey.Insert(0, 0x00);
        AppendLength(ref arrBinaryPrivateKey, 1);
        arrBinaryPrivateKey.Insert(0, INTEGER);

        AppendLength(ref arrBinaryPrivateKey, arrBinaryPrivateKey.Count);
        arrBinaryPrivateKey.Insert(0, SEQUENCE);

        return System.Convert.ToBase64String(arrBinaryPrivateKey.ToArray());
    }

    public static string GetPublicKeyInBinary(string base64PrivateKey)
    {
        return ConvertKey(base64PrivateKey, false);
    }

    public static string GetPrivateKeyInBinary(string base64PrivateKey)
    {
        return ConvertKey(base64PrivateKey, true);
    }

    private static string ConvertKey(string base64PrivateKey, bool includePrivateKey)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(rsaKeySize))
        {
            try
            {
                rsa.FromXmlString(base64PrivateKey.Base64ToNormalString());

                if (includePrivateKey)
                {
                    return GetPrivateKeyInBinary(rsa.ExportParameters(includePrivateKey));
                }
                else
                {
                    return GetPublicKeyInBinary(rsa.ExportParameters(includePrivateKey));
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }

    #endregion


    #region Private Methods

    private static void AppendLength(ref List<byte> arrBinaryData, int nLen)
    {
        if (nLen <= byte.MaxValue)
        {
            arrBinaryData.Insert(0, Convert.ToByte(nLen));
            arrBinaryData.Insert(0, 0x81); //This byte means that the length fits in one byte
        }
        else
        {
            arrBinaryData.Insert(0, Convert.ToByte(nLen % (byte.MaxValue + 1)));
            arrBinaryData.Insert(0, Convert.ToByte(nLen / (byte.MaxValue + 1)));
            arrBinaryData.Insert(0, 0x82); //This byte means that the length fits in two byte
        }
    }

    #endregion
}
