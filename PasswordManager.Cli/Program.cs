using System;
using PasswordManager.Cli.Services;
using PasswordManager.Cli.Storage;

namespace PasswordManager.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Password Manager CLI");
            string vaultPath = Environment.GetEnvironmentVariable("PM_VAULT") ?? "/home/jota/Proyectos/PasswordManager/.vault";

            Console.Write("Introduce la contraseña maestra: ");
            string? master = ReadSecret();
            if (string.IsNullOrWhiteSpace(master))
            {
                Console.WriteLine("Contraseña inválida");
                return;
            }

            var repo = new VaultRepository(vaultPath);
            var service = new VaultService(repo);
            service.Initialize(master);

            if (args.Length == 0)
            {
                Console.WriteLine("Comandos: add, list, remove <id>");
                return;
            }

            switch (args[0])
            {
                case "add":
                    Console.Write("Servicio: ");
                    string serviceName = Console.ReadLine() ?? string.Empty;
                    Console.Write("Usuario: ");
                    string username = Console.ReadLine() ?? string.Empty;
                    Console.Write("Password: ");
                    string? password = ReadSecret();
                    Console.Write("Notas (opcional): ");
                    string? notes = Console.ReadLine();
                    var entry = service.AddEntry(serviceName, username, password ?? string.Empty, notes);
                    Console.WriteLine($"Creado: {entry.Id}");
                    break;
                case "list":
                    foreach (var e in service.ListEntries())
                    {
                        Console.WriteLine($"{e.Id} | {e.Service} | {e.Username} | {e.Password}");
                    }
                    break;
                case "remove":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Uso: remove <id>");
                        return;
                    }
                    bool removed = service.RemoveEntry(args[1]);
                    Console.WriteLine(removed ? "Eliminado" : "ID no encontrado");
                    break;
                default:
                    Console.WriteLine("Comando no reconocido");
                    break;
            }
        }

        private static string? ReadSecret()
        {
            string secret = string.Empty;
            ConsoleKeyInfo k;
            while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (k.Key == ConsoleKey.Backspace)
                {
                    if (secret.Length > 0)
                    {
                        secret = secret.Substring(0, secret.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(k.KeyChar))
                {
                    secret += k.KeyChar;
                    Console.Write('*');
                }
            }
            Console.WriteLine();
            return secret;
        }
    }
}
