using System;

namespace PasswordManager.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Password Manager CLI - Inicializado");
            Console.WriteLine("Módulos criptográficos disponibles: PBKDF2, AES-GCM, RSA");
        }
    }
}
