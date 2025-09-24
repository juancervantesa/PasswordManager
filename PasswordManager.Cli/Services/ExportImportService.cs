using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.Json;
using PasswordManager.Cli.Crypto;
using PasswordManager.Cli.Models;

namespace PasswordManager.Cli.Services
{
    public sealed class ExportImportService
    {
        public void GenerateKeyPair(string publicKeyPath, string privateKeyPath)
        {
            var (pub, priv) = RsaToolbox.GenerateRsaKeyPair();
            File.WriteAllBytes(publicKeyPath, pub);
            File.WriteAllBytes(privateKeyPath, priv);
        }

        public void ExportEntry(PasswordEntry entry, string publicKeyPath, string outputPath)
        {
            byte[] publicKey = ReadKeyAuto(publicKeyPath, isPublic: true);

            // 1) Serializar entrada
            string json = JsonSerializer.Serialize(entry);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            // 2) Generar clave simétrica y cifrar con AES-GCM
            byte[] aesKey = new byte[32]; // AES-256
            System.Security.Cryptography.RandomNumberGenerator.Fill(aesKey);
            byte[] aesPayload = AesGcmCipher.Encrypt(aesKey, plaintext);

            // 3) Cifrar la clave AES con RSA-OAEP(SHA-256)
            byte[] rsaEncryptedKey = RsaToolbox.EncryptWithPublicKey(publicKey, aesKey);

            // 4) Empaquetar: [4 bytes len RSA][RSA-blob][AES-payload]
            byte[] output = new byte[4 + rsaEncryptedKey.Length + aesPayload.Length];
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(0, 4), rsaEncryptedKey.Length);
            Buffer.BlockCopy(rsaEncryptedKey, 0, output, 4, rsaEncryptedKey.Length);
            Buffer.BlockCopy(aesPayload, 0, output, 4 + rsaEncryptedKey.Length, aesPayload.Length);

            File.WriteAllBytes(outputPath, output);
        }

        public PasswordEntry ImportEntry(string privateKeyPath, string inputPath)
        {
            byte[] privateKey = ReadKeyAuto(privateKeyPath, isPublic: false);
            byte[] input = File.ReadAllBytes(inputPath);

            if (input.Length < 4)
                throw new InvalidDataException("Archivo de entrada inválido");

            int rsaLen = BinaryPrimitives.ReadInt32BigEndian(input.AsSpan(0, 4));
            if (rsaLen <= 0 || 4 + rsaLen > input.Length)
                throw new InvalidDataException("Archivo de entrada corrupto (longitud RSA)");

            byte[] rsaEncryptedKey = new byte[rsaLen];
            Buffer.BlockCopy(input, 4, rsaEncryptedKey, 0, rsaLen);
            int aesPayloadLen = input.Length - 4 - rsaLen;
            byte[] aesPayload = new byte[aesPayloadLen];
            Buffer.BlockCopy(input, 4 + rsaLen, aesPayload, 0, aesPayloadLen);

            // 1) Descifrar clave AES con RSA
            byte[] aesKey = RsaToolbox.DecryptWithPrivateKey(privateKey, rsaEncryptedKey);

            // 2) Descifrar payload AES-GCM
            byte[] plaintext = AesGcmCipher.Decrypt(aesKey, aesPayload);

            string json = Encoding.UTF8.GetString(plaintext);
            var entry = JsonSerializer.Deserialize<PasswordEntry>(json) ?? throw new InvalidDataException("Entrada inválida");
            // regenerar Id para evitar colisiones
            entry.Id = Guid.NewGuid().ToString("n");
            entry.CreatedAtUtc = DateTime.UtcNow;
            entry.UpdatedAtUtc = DateTime.UtcNow;
            return entry;
        }

        private static byte[] ReadKeyAuto(string path, bool isPublic)
        {
            // Permitir DER binario (como genera el comando "genkeys") o PEM en texto
            byte[] raw = File.ReadAllBytes(path);
            // Heurística simple: si contiene "BEGIN" tratamos como PEM
            if (LooksLikePem(raw))
            {
                string pem = File.ReadAllText(path);
                return isPublic ? RsaToolbox.ImportPublicKeyFromPem(pem)
                                : RsaToolbox.ImportPrivateKeyFromPem(pem);
            }
            return raw;
        }

        private static bool LooksLikePem(byte[] content)
        {
            // Buscar la secuencia "BEGIN" en los primeros bytes
            int max = Math.Min(content.Length, 64);
            for (int i = 0; i < max - 4; i++)
            {
                if (content[i] == (byte)'B' && content[i + 1] == (byte)'E' && content[i + 2] == (byte)'G' && content[i + 3] == (byte)'I' && content[i + 4] == (byte)'N')
                {
                    return true;
                }
            }
            return false;
        }
    }
}



