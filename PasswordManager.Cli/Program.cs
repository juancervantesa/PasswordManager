using System;
using PasswordManager.Cli.Services;
using PasswordManager.Cli.Storage;
using PasswordManager.Cli.Config;
using PasswordManager.Cli.Models;
using PasswordManager.Cli.Crypto;
using System.Text;

namespace PasswordManager.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            /* //---------------
            // 1. Generar claves
            var (pub, priv) = RsaToolbox.GenerateRsaKeyPair();

            // 2. Exportar a PEM
            string pubPem = RsaToolbox.ExportPublicKeyToPem(pub);
            string privPem = RsaToolbox.ExportPrivateKeyToPem(priv);

            Console.WriteLine("Clave pública PEM:\n" + pubPem);
            Console.WriteLine("Clave privada PEM:\n" + privPem);

            // 3. Importar de nuevo desde PEM
            byte[] pub2 = RsaToolbox.ImportPublicKeyFromPem(pubPem);
            byte[] priv2 = RsaToolbox.ImportPrivateKeyFromPem(privPem);

            // 4. Cifrar y descifrar
            var mensaje = "Hola mundo con PEM!";
            var encrypted = RsaToolbox.EncryptWithPublicKey(pub2, Encoding.UTF8.GetBytes(mensaje));
            var decrypted = RsaToolbox.DecryptWithPrivateKey(priv2, encrypted);

            Console.WriteLine("Descifrado: " + Encoding.UTF8.GetString(decrypted));
            //---------------- */


            Console.WriteLine("Password Manager CLI");
            var cfg = AppConfig.Load(Environment.CurrentDirectory);
            //string vaultPath = string.IsNullOrWhiteSpace(cfg.VaultPath) ? "D:\\cursos\\Maestria\\Modulo 7\\PasswordManager\\.vault" : cfg.VaultPath!;
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
                RunMenu(service);
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
        public static string Enmascarar(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;
            return new string('*', password.Length);
        }

    


        private static void RunMenu(Services.VaultService service)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=== Password Manager ===");
                Console.WriteLine("1) Listar contraseñas");
                Console.WriteLine("2) Agregar contraseña");
                Console.WriteLine("3) Eliminar contraseña");
                Console.WriteLine("4) Generar contraseña segura");
                Console.WriteLine("5) Generar par de claves RSA");
                Console.WriteLine("6) Exportar entrada (RSA)");
                Console.WriteLine("7) Importar entrada (RSA)");
                Console.WriteLine("8) Copiar Contraseña");

                Console.WriteLine("0) Salir");
                Console.Write("Selecciona una opción: ");
                string? opt = Console.ReadLine();
                Console.WriteLine();

                switch (opt)
                {
                    case "1":
                        // Definir anchos de columna
                        int idWidth = 32;
                        int serviceWidth = 18;
                        int userWidth = 18;
                        int passWidth = 16;
                        // Encabezados
                        Console.WriteLine(
                            $"{"ID".PadRight(idWidth)} | {"Servicio".PadRight(serviceWidth)} | {"Usuario".PadRight(userWidth)} | {"Contraseña".PadRight(passWidth)}"
                        );
                        Console.WriteLine(new string('-', idWidth + serviceWidth + userWidth + passWidth + 9));

                        foreach (var e in service.ListEntries())
                        {
                            Console.WriteLine(
                             $"{e.Id.PadRight(idWidth)} | {e.Service.PadRight(serviceWidth)} | {e.Username.PadRight(userWidth)} | {Enmascarar(e.Password).PadRight(passWidth)}"
               );

                           // Console.WriteLine($" Id: {e.Id} | Sitio: {e.Service} | Usuario: {e.Username} | Contraseña:  {Enmascarar(e.Password)}");
                           
                               // Console.WriteLine($"Sitio: {entry.Site}, Usuario: {entry.Username}, Contraseña: {Enmascarar(entry.Password)}");
                          

                        }
                        break;
                    case "2":
                        Console.Write("Servicio: ");
                        string serviceName = Console.ReadLine() ?? string.Empty;
                        Console.Write("Usuario: ");
                        string username = Console.ReadLine() ?? string.Empty;
                        Console.Write("Password (vacío para generar): ");
                        string? pwd = ReadSecret();
                        if (string.IsNullOrEmpty(pwd))
                        {
                            pwd = Services.PasswordGenerator.Generate(16);
                            Console.WriteLine($"\nGenerada: {pwd}");
                        }
                        Console.Write("Notas (opcional): ");
                        string? notes = Console.ReadLine();
                        var created = service.AddEntry(serviceName, username, pwd, notes);
                        Console.WriteLine($"Creado: {created.Id}");
                        break;
                    case "3":
                        Console.Write("ID a eliminar: ");
                        string id = Console.ReadLine() ?? string.Empty;
                        bool removed = service.RemoveEntry(id);
                        Console.WriteLine(removed ? "Eliminado" : "ID no encontrado");
                        break;
                    case "4":
                        Console.Write("Longitud (por defecto 16): ");
                        string? lenStr = Console.ReadLine();
                        int len = 16;
                        if (!string.IsNullOrWhiteSpace(lenStr) && int.TryParse(lenStr, out int parsed)) len = parsed;
                        string generated = Services.PasswordGenerator.Generate(len);
                        Console.WriteLine(generated);
                        break;
                    case "5":
                        Console.Write("Ruta clave pública: ");
                        string pub = Console.ReadLine() ?? string.Empty;
                        Console.Write("Ruta clave privada: ");
                        string priv = Console.ReadLine() ?? string.Empty;
                        new Services.ExportImportService().GenerateKeyPair(pub, priv);
                        Console.WriteLine("Par de claves generado");
                        break;
                    case "6":
                        Console.Write("ID a exportar: ");
                        string expId = Console.ReadLine() ?? string.Empty;
                        var entry = System.Linq.Enumerable.FirstOrDefault(service.ListEntries(), e => e.Id == expId);
                        if (entry == null)
                        {
                            Console.WriteLine("ID no encontrado");
                            break;
                        }
                        Console.Write("Ruta clave pública: ");
                        string expPub = Console.ReadLine() ?? string.Empty;
                        Console.Write("Archivo de salida: ");
                        string expOut = Console.ReadLine() ?? string.Empty;
                        new Services.ExportImportService().ExportEntry(entry, expPub, expOut);
                        Console.WriteLine("Entrada exportada");
                        break;
                    case "7":
                        Console.Write("Ruta clave privada: ");
                        string impPriv = Console.ReadLine() ?? string.Empty;
                        Console.Write("Archivo de entrada: ");
                        string impIn = Console.ReadLine() ?? string.Empty;
                        var imported = new Services.ExportImportService().ImportEntry(impPriv, impIn);
                        imported = service.AddEntry(imported.Service, imported.Username, imported.Password, imported.Notes);
                        Console.WriteLine($"Importado: {imported.Id}");
                        break;
                    case "8":
                        Console.Write("ID de la contraseña a copiar: ");
                        string idCopy = Console.ReadLine() ?? string.Empty;
                        bool copy = service.CopyEntry(idCopy);
                        Console.WriteLine("Contraseña copiada al portapapeles.");
                        break;


                    case "0":
                        return;
                    default:
                        Console.WriteLine("Opción inválida");
                        break;
                }
            }
        }
    }
}
