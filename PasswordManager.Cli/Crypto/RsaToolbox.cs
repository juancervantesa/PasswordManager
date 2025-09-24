using System;
using System.Security.Cryptography;

namespace PasswordManager.Cli.Crypto
{

    //// Utilidades para generar, exportar e importar pares de claves RSA y para cifrar/firmar.

    //public static class RsaToolbox
    //{
    //    public static (byte[] publicKey, byte[] privateKey) GenerateRsaKeyPair(int keySize = 2048)
    //    {
    //        using var rsa = RSA.Create(keySize);
    //        return (rsa.ExportSubjectPublicKeyInfo(), rsa.ExportPkcs8PrivateKey());
    //    }

    //    public static byte[] EncryptWithPublicKey(byte[] publicKey, byte[] data)
    //    {
    //        using var rsa = RSA.Create();
    //        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
    //        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    //    }

    //    public static byte[] DecryptWithPrivateKey(byte[] privateKey, byte[] encrypted)
    //    {
    //        using var rsa = RSA.Create();
    //        rsa.ImportPkcs8PrivateKey(privateKey, out _);
    //        return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
    //    }

    //    public static byte[] Sign(byte[] privateKey, byte[] data)
    //    {
    //        using var rsa = RSA.Create();
    //        rsa.ImportPkcs8PrivateKey(privateKey, out _);
    //        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    //    }

    //    public static bool Verify(byte[] publicKey, byte[] data, byte[] signature)
    //    {
    //        using var rsa = RSA.Create();
    //        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
    //        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    //    }
    //}

    public static class RsaToolbox
    {
        // ======================
        // Generaci�n de claves
        // ======================
        public static (byte[] publicKey, byte[] privateKey) GenerateRsaKeyPair(int keySize = 2048)
        {
            using var rsa = RSA.Create(keySize);
            return (rsa.ExportSubjectPublicKeyInfo(), rsa.ExportPkcs8PrivateKey());
        }

        // ======================
        // Cifrado / Descifrado
        // ======================
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

        // ======================
        // Firmar / Verificar
        // ======================
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

        // ======================
        // Exportar a PEM
        // ======================
        public static string ExportPublicKeyToPem(byte[] publicKey)
        {
            return ToPem(publicKey, "PUBLIC KEY");
        }

        public static string ExportPrivateKeyToPem(byte[] privateKey)
        {
            return ToPem(privateKey, "PRIVATE KEY");
        }

        private static string ToPem(byte[] derBytes, string header)
        {
            var base64 = Convert.ToBase64String(derBytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN {header}-----\n{base64}\n-----END {header}-----";
        }

        // ======================
        // Importar desde PEM
        // ======================
        public static byte[] ImportPublicKeyFromPem(string pem)
        {
            return FromPem(pem, "PUBLIC KEY");
        }

        public static byte[] ImportPrivateKeyFromPem(string pem)
        {
            return FromPem(pem, "PRIVATE KEY");
        }

        private static byte[] FromPem(string pem, string header)
        {
            var base64 = pem.Replace($"-----BEGIN {header}-----", "")
                            .Replace($"-----END {header}-----", "")
                            .Replace("\n", "")
                            .Replace("\r", "")
                            .Trim();
            return Convert.FromBase64String(base64);
        }
    }
}



