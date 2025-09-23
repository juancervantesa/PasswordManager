using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PasswordManager.Cli.Crypto;
using PasswordManager.Cli.Models;

namespace PasswordManager.Cli.Storage
{
    public sealed class VaultRepository
    {
        private readonly string _vaultPath;

        public VaultRepository(string vaultPath)
        {
            _vaultPath = vaultPath;
        }

        public void InitializeIfMissing(string masterPassword)
        {
            if (File.Exists(_vaultPath)) return;

            byte[] salt = KeyDerivation.GenerateRandomSalt();
            byte[] key = KeyDerivation.DeriveKeyFromPassword(masterPassword, salt);
            byte[] verifier = KeyDerivation.ComputePasswordHash(masterPassword, salt);

            Vault newVault = new Vault
            {
                Salt = salt,
                PasswordVerifier = verifier,
            };

            SaveVault(newVault, key);
        }

        public Vault LoadVault(string masterPassword)
        {
            byte[] raw = File.ReadAllBytes(_vaultPath);
            // Cabecera: 4 bytes magic + 1 byte version + 4 bytes leng JSON + blob cifrado
            if (raw.Length < 9) throw new InvalidDataException("Vault corrupto");
            if (raw[0] != (byte)'P' || raw[1] != (byte)'M' || raw[2] != (byte)'V' || raw[3] != (byte)'1')
                throw new InvalidDataException("Formato de vault desconocido");

            int jsonLength = BitConverter.ToInt32(raw, 5);
            int offset = 9;
            if (jsonLength <= 0 || raw.Length < offset + jsonLength) throw new InvalidDataException("Longitud inválida");

            byte[] metadataJsonBytes = new byte[jsonLength];
            Buffer.BlockCopy(raw, offset, metadataJsonBytes, 0, jsonLength);
            offset += jsonLength;

            byte[] encryptedBlob = new byte[raw.Length - offset];
            Buffer.BlockCopy(raw, offset, encryptedBlob, 0, encryptedBlob.Length);

            var metadata = JsonSerializer.Deserialize<Vault>(metadataJsonBytes) ?? throw new InvalidDataException("Metadata inválida");

            byte[] key = KeyDerivation.DeriveKeyFromPassword(masterPassword, metadata.Salt);
            // Verificación rápida de contraseña
            byte[] expected = KeyDerivation.ComputePasswordHash(masterPassword, metadata.Salt);
            if (!CryptographicOperations.FixedTimeEquals(expected, metadata.PasswordVerifier))
                throw new UnauthorizedAccessException("Contraseña maestra incorrecta");

            byte[] plaintext = AesGcmCipher.Decrypt(key, encryptedBlob);
            string entriesJson = Encoding.UTF8.GetString(plaintext);
            var entries = JsonSerializer.Deserialize<PasswordEntry[]>(entriesJson) ?? Array.Empty<PasswordEntry>();

            metadata.Entries.Clear();
            metadata.Entries.AddRange(entries);
            return metadata;
        }

        public void SaveVault(Vault vault, byte[] key)
        {
            string entriesJson = JsonSerializer.Serialize(vault.Entries);
            byte[] plaintext = Encoding.UTF8.GetBytes(entriesJson);
            byte[] encrypted = AesGcmCipher.Encrypt(key, plaintext);

            // Serializar metadata sin las entradas
            var metadata = new Vault
            {
                Version = vault.Version,
                Salt = vault.Salt,
                PasswordVerifier = vault.PasswordVerifier,
                Entries = new System.Collections.Generic.List<PasswordEntry>()
            };
            byte[] metadataJsonBytes = JsonSerializer.SerializeToUtf8Bytes(metadata);

            byte[] output = new byte[4 + 1 + 4 + metadataJsonBytes.Length + encrypted.Length];
            // Magic 'PMV1'
            output[0] = (byte)'P';
            output[1] = (byte)'M';
            output[2] = (byte)'V';
            output[3] = (byte)'1';
            // Version
            output[4] = 1;
            // Longitud metadata JSON
            byte[] len = BitConverter.GetBytes(metadataJsonBytes.Length);
            Buffer.BlockCopy(len, 0, output, 5, 4);
            // Metadata
            Buffer.BlockCopy(metadataJsonBytes, 0, output, 9, metadataJsonBytes.Length);
            // Blob cifrado
            Buffer.BlockCopy(encrypted, 0, output, 9 + metadataJsonBytes.Length, encrypted.Length);

            Directory.CreateDirectory(Path.GetDirectoryName(_vaultPath)!);
            File.WriteAllBytes(_vaultPath, output);
        }
    }
}


