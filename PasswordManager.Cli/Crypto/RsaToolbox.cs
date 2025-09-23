using System;
using System.Security.Cryptography;

namespace PasswordManager.Cli.Crypto
{
    /// <summary>
    /// Utilidades para generar, exportar e importar pares de claves RSA y para cifrar/firmar.
    /// </summary>
    public static class RsaToolbox
    {
        public static (byte[] publicKey, byte[] privateKey) GenerateRsaKeyPair(int keySize = 2048)
        {
            using var rsa = RSA.Create(keySize);
            return (rsa.ExportSubjectPublicKeyInfo(), rsa.ExportPkcs8PrivateKey());
        }

        public static byte[] EncryptWithPublicKey(byte[] publicKey, byte[] data)
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] DecryptWithPrivateKey(byte[] privateKey, byte[] encrypted)
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
        }

        public static byte[] Sign(byte[] privateKey, byte[] data)
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public static bool Verify(byte[] publicKey, byte[] data, byte[] signature)
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}


