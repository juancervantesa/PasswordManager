using System;
using System.IO;
using System.Text.Json;

namespace PasswordManager.Cli.Config
{
    public sealed class AppConfig
    {
        public string? VaultPath { get; set; }

        public static AppConfig Load(string workingDirectory)
        {
            string envVault = Environment.GetEnvironmentVariable("PM_VAULT") ?? string.Empty;
            string configPath = Path.Combine(workingDirectory, "pmconfig.json");

            AppConfig cfg = new AppConfig();
            if (File.Exists(configPath))
            {
                try
                {
                    cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath)) ?? new AppConfig();
                }
                catch
                {
                    cfg = new AppConfig();
                }
            }

            if (!string.IsNullOrWhiteSpace(envVault)) cfg.VaultPath = envVault;

            return cfg;
        }
    }
}



