using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Cli.Services
{
    public static class PasswordGenerator
    {
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?/";

        public static string Generate(int length = 16, bool includeUpper = true, bool includeDigits = true, bool includeSymbols = true)
        {
            if (length < 8) throw new ArgumentException("Longitud mínima 8", nameof(length));

            StringBuilder pool = new StringBuilder(Lower);
            if (includeUpper) pool.Append(Upper);
            if (includeDigits) pool.Append(Digits);
            if (includeSymbols) pool.Append(Symbols);

            string poolStr = pool.ToString();
            if (string.IsNullOrEmpty(poolStr)) throw new InvalidOperationException("Pool vacío");

            Span<char> result = length <= 256 ? stackalloc char[length] : new char[length];

            // Garantizar al menos un carácter de cada categoría seleccionada
            int index = 0;
            result[index++] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
            if (includeUpper) result[index++] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
            if (includeDigits) result[index++] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            if (includeSymbols) result[index++] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];

            for (; index < length; index++)
            {
                int r = RandomNumberGenerator.GetInt32(poolStr.Length);
                result[index] = poolStr[r];
            }

            // Barajar
            for (int i = result.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return new string(result);
        }
    }
}


