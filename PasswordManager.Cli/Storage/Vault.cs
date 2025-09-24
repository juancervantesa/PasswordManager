using System;
using System.Collections.Generic;
using PasswordManager.Cli.Models;

namespace PasswordManager.Cli.Storage
{
    public sealed class Vault
    {
        public string Version { get; set; } = "1";
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public byte[] PasswordVerifier { get; set; } = Array.Empty<byte>();
        public List<PasswordEntry> Entries { get; set; } = new List<PasswordEntry>();
    }
}



