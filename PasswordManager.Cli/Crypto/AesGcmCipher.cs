using System;
using System.Security.Cryptography;

namespace PasswordManager.Cli.Crypto
{
    /// <summary>
    /// Cifrado y descifrado con AES-GCM (AEAD). Requiere clave de 16/24/32 bytes.
    /// Empaqueta: nonce (12), tag (16) y ciphertext.
    /// </summary>
    public static class AesGcmCipher
    {
        public static byte[] Encrypt(byte[] key, byte[] plaintext, byte[]? associatedData = null)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("Clave AES inválida", nameof(key));
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using var aesGcm = new AesGcm(key);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            byte[] output = new byte[12 + 16 + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, output, 0, 12);
            Buffer.BlockCopy(tag, 0, output, 12, 16);
            Buffer.BlockCopy(ciphertext, 0, output, 28, ciphertext.Length);
            return output;
        }

        public static byte[] Decrypt(byte[] key, byte[] input, byte[]? associatedData = null)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("Clave AES inválida", nameof(key));
            if (input == null || input.Length < 28) throw new ArgumentException("Entrada inválida", nameof(input));

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            int ciphertextLength = input.Length - 28;
            byte[] ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(input, 0, nonce, 0, 12);
            Buffer.BlockCopy(input, 12, tag, 0, 16);
            Buffer.BlockCopy(input, 28, ciphertext, 0, ciphertextLength);

            byte[] plaintext = new byte[ciphertextLength];
            using var aesGcm = new AesGcm(key);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            return plaintext;
        }
    }
}


