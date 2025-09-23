using System;
using PasswordManager.Cli.Services;
using PasswordManager.Cli.Storage;
using PasswordManager.Cli.Config;

namespace PasswordManager.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Password Manager CLI");
            var cfg = AppConfig.Load(Environment.CurrentDirectory);
            string vaultPath = string.IsNullOrWhiteSpace(cfg.VaultPath) ? "/home/jota/Proyectos/PasswordManager/.vault" : cfg.VaultPath!;

            string? master = Environment.GetEnvironmentVariable("PM_PASSWORD");
            if (string.IsNullOrEmpty(master))
            {
                Console.Write("Introduce la contraseña maestra: ");
                master = ReadSecret();
            }
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
                Console.WriteLine("Comandos: add, list, remove <id>, genpass [len], genkeys <pub> <priv>, export <id> <pub> <out>, import <priv> <in>");
                return;
            }

            switch (args[0])
            {
                case "add":
                    // Flags/env: --service, --username, --password, --notes o PM_SERVICE, PM_USERNAME, PM_ENTRY_PASSWORD, PM_NOTES
                    string serviceName = GetFlag(args, "--service") ?? Environment.GetEnvironmentVariable("PM_SERVICE") ?? string.Empty;
                    string username = GetFlag(args, "--username") ?? Environment.GetEnvironmentVariable("PM_USERNAME") ?? string.Empty;
                    string? password = GetFlag(args, "--password") ?? Environment.GetEnvironmentVariable("PM_ENTRY_PASSWORD");
                    string? notes = GetFlag(args, "--notes") ?? Environment.GetEnvironmentVariable("PM_NOTES");
                    if (string.IsNullOrWhiteSpace(serviceName))
                    {
                        Console.Write("Servicio: ");
                        serviceName = Console.ReadLine() ?? string.Empty;
                    }
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        Console.Write("Usuario: ");
                        username = Console.ReadLine() ?? string.Empty;
                    }
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.Write("Password: ");
                        password = ReadSecret();
                    }
                    if (notes == null)
                    {
                        Console.Write("Notas (opcional): ");
                        notes = Console.ReadLine();
                    }
                    var entry = service.AddEntry(serviceName, username, password ?? string.Empty, notes);
                    Console.WriteLine($"Creado: {entry.Id}");
                    break;
                case "list":
                    foreach (var e in service.ListEntries())
                    {
                        Console.WriteLine($"{e.Id} | {e.Service} | {e.Username} | {e.Password}");
                    }
                    break;
                case "genpass":
                    int len = 16;
                    if (args.Length >= 2 && int.TryParse(args[1], out int parsed)) len = parsed;
                    string generated = PasswordManager.Cli.Services.PasswordGenerator.Generate(len);
                    Console.WriteLine(generated);
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
                case "genkeys":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Uso: genkeys <ruta_pub> <ruta_priv>");
                        return;
                    }
                    new ExportImportService().GenerateKeyPair(args[1], args[2]);
                    Console.WriteLine("Par de claves generado");
                    break;
                case "export":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Uso: export <id> <ruta_pub> <salida>");
                        return;
                    }
                    var entryToExport = System.Linq.Enumerable.FirstOrDefault(service.ListEntries(), e => e.Id == args[1]);
                    if (entryToExport == null)
                    {
                        Console.WriteLine("ID no encontrado");
                        return;
                    }
                    new ExportImportService().ExportEntry(entryToExport, args[2], args[3]);
                    Console.WriteLine("Entrada exportada");
                    break;
                case "import":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Uso: import <ruta_priv> <entrada>");
                        return;
                    }
                    var imported = new ExportImportService().ImportEntry(args[1], args[2]);
                    imported = service.AddEntry(imported.Service, imported.Username, imported.Password, imported.Notes);
                    Console.WriteLine($"Importado: {imported.Id}");
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

        private static string? GetFlag(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
                if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i].Substring(name.Length + 1);
                }
            }
            // manejar último arg como name=value
            if (args.Length == 1 && args[0].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                return args[0].Substring(name.Length + 1);
            }
            return null;
        }
    }
}
