using System;
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
            byte[] pub = File.ReadAllBytes(publicKeyPath);
            string json = JsonSerializer.Serialize(entry);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = RsaToolbox.EncryptWithPublicKey(pub, data);
            File.WriteAllBytes(outputPath, encrypted);
        }

        public PasswordEntry ImportEntry(string privateKeyPath, string inputPath)
        {
            byte[] priv = File.ReadAllBytes(privateKeyPath);
            byte[] encrypted = File.ReadAllBytes(inputPath);
            byte[] data = RsaToolbox.DecryptWithPrivateKey(priv, encrypted);
            string json = Encoding.UTF8.GetString(data);
            var entry = JsonSerializer.Deserialize<PasswordEntry>(json) ?? throw new InvalidDataException("Entrada inválida");
            // regenerar Id para evitar colisiones
            entry.Id = Guid.NewGuid().ToString("n");
            entry.CreatedAtUtc = DateTime.UtcNow;
            entry.UpdatedAtUtc = DateTime.UtcNow;
            return entry;
        }
    }
}


