using System;

namespace PasswordManager.Cli.Models
{
    public sealed class PasswordEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public string Service { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}



