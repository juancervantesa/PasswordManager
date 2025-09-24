using System;
using System.Collections.Generic;
using System.Linq;
using PasswordManager.Cli.Crypto;
using PasswordManager.Cli.Models;
using PasswordManager.Cli.Storage;
using TextCopy;

namespace PasswordManager.Cli.Services
{
    public sealed class VaultService
    {
        private readonly VaultRepository _repository;
        private Vault? _cachedVault;
        private byte[]? _cachedKey;

        public VaultService(VaultRepository repository)
        {
            _repository = repository;
        }

        public void Initialize(string masterPassword)
        {
            _repository.InitializeIfMissing(masterPassword);
            Load(masterPassword);
        }

        public void Load(string masterPassword)
        {
            _cachedVault = _repository.LoadVault(masterPassword);
            _cachedKey = KeyDerivation.DeriveKeyFromPassword(masterPassword, _cachedVault.Salt);
        }

        public IEnumerable<PasswordEntry> ListEntries() => _cachedVault?.Entries ?? Enumerable.Empty<PasswordEntry>();

        public PasswordEntry AddEntry(string service, string username, string password, string? notes)
        {
            EnsureLoaded();
            var entry = new PasswordEntry
            {
                Service = service,
                Username = username,
                Password = password,
                Notes = notes,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _cachedVault!.Entries.Add(entry);
            Persist();
            return entry;
        }

        public bool RemoveEntry(string id)
        {
            EnsureLoaded();
            var entry = _cachedVault!.Entries.FirstOrDefault(e => e.Id == id);
            if (entry == null) return false;
            _cachedVault!.Entries.Remove(entry);
            Persist();
            return true;
        }
        public bool CopyEntry(string id)
        {
            EnsureLoaded();
            var entry = _cachedVault!.Entries.FirstOrDefault(e => e.Id == id);
            if (entry == null) return false;
            TextCopy.ClipboardService.SetText(entry.Password);
            
            return true;
        }


        private void Persist()
        {
            EnsureLoaded();
            _repository.SaveVault(_cachedVault!, _cachedKey!);
        }

        private void EnsureLoaded()
        {
            if (_cachedVault == null || _cachedKey == null) throw new InvalidOperationException("Vault no cargado");
        }
    }
}



