using System;
using System.Security.Cryptography;

namespace PasswordManager.Cli.Crypto
{
    /// <summary>
    /// Deriva claves simétricas a partir de una contraseña usando PBKDF2 con HMACSHA256.
    /// </summary>
    public static class KeyDerivation
    {
        public static byte[] GenerateRandomSalt(int saltLengthBytes = 16)
        {
            byte[] salt = new byte[saltLengthBytes];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        public static byte[] DeriveKeyFromPassword(string password, byte[] salt, int keyLengthBytes = 32, int iterations = 210_000)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password no puede ser vacío", nameof(password));
            if (salt == null || salt.Length < 8) throw new ArgumentException("Salt inválido", nameof(salt));

            using Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keyLengthBytes);
        }

        public static byte[] ComputePasswordHash(string password, byte[] salt, int iterations = 210_000)
        {
            // Hash para verificar contraseña maestra (no para cifrado)
            using var hmac = new HMACSHA256(DeriveKeyFromPassword(password, salt, 32, iterations));
            return hmac.ComputeHash(salt);
        }
    }
}


