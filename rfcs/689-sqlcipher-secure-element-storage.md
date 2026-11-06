- Feature Name: `sqlite_sqlcipher_secure_element_storage`
- Start Date: 2026-06-17
- RFC PR: [fiskaltrust/middleware#689](https://github.com/fiskaltrust/middleware/pull/689)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: `AT`, `DE`, `FR`, `IT`, `ME`

# Summary

Introduce a SQLCipher-backed SQLite storage mode for the Middleware (`sqlite+sqlcipher://...`) and define how the Launcher resolves the encryption key from a local OS-protected secret store / secure element before injecting it into the in-memory CashBox configuration.

The goal is to keep the storage key off disk and out of command-line arguments while preserving the existing Middleware repository model and launcher-based deployment flow.

# Motivation

SQLite is currently the simplest local storage backend, but plain SQLite does not encrypt the database file at rest. For deployments where the queue database may contain sensitive operational or fiscal data, the file itself should be unreadable without an additional secret.

SQLCipher provides transparent encryption for SQLite databases. The missing piece is key management: the key must be available locally, but must not be stored as a plaintext file or in launcher process arguments.

This RFC proposes a cross-platform solution for deriving or unwrapping the SQLCipher key from local platform facilities:

- Windows: BitLocker for the volume plus DPAPI/TPM protection for the wrapped SQLCipher key.
- Android: Android Keystore backed by StrongBox / secure element where available.
- macOS: Keychain with Secure Enclave-backed items where available, plus FileVault for disk protection.
- Linux: TPM2-bound credentials via `systemd-creds` / `tpm2-tools` for service deployments; desktop fallback to Secret Service providers only when TPM2 is unavailable.

# Guide-level explanation

## What changes for PosCreators and operators?

Nothing changes about receipt semantics, journals, or country-specific fiscalization logic.
What changes is how the Middleware stores queue state locally.

Instead of using plain SQLite, operators can choose a SQLCipher-backed SQLite storage variant.
The Launcher reads the database key from the local OS-protected secret store, injects it into the runtime configuration, and starts the queue with an encrypted database file.

The database path is still local, but the file contents are encrypted.
Backups are therefore also encrypted, and restore requires the same key material.

## How to think about the new storage mode

Think of the storage setup as having two layers:

1. **Disk protection**: BitLocker, FileVault, LUKS, or Android device encryption protect the whole device.
2. **Database protection**: SQLCipher protects the individual SQLite file with its own key.

Both layers matter, but they solve different problems.
Disk encryption protects a powered-off device.
SQLCipher protects copied files, backups, and exposed volumes.

The key itself is not stored in the configuration file in plaintext.
Instead, the configuration contains a reference to a key alias or key slot, and the Launcher resolves it at runtime.

## Platform recommendations

| Platform | Preferred local secret technology | Secondary protection | Notes |
|---|---|---|---|
| Windows | DPAPI + TPM-sealed wrapped secret | BitLocker | BitLocker alone is not sufficient for key management; use it to protect the volume and DPAPI/TPM to protect the SQLCipher key material. |
| Android | Android Keystore / StrongBox | Device encryption | Store a wrapped SQLCipher key or generate a wrapping key in the keystore. Prefer hardware-backed keys when available. |
| macOS | Keychain + Secure Enclave-backed items | FileVault | FileVault protects the volume; Keychain stores the wrapped SQLCipher key. Secure Enclave is preferred when available. |
| Linux | TPM2-bound secret via `systemd-creds` / `tpm2-tools` | LUKS / full-disk encryption | For service deployments, TPM2-bound credentials are the cleanest local-secure-element approach. Desktop keyring fallback is acceptable only when TPM2 is unavailable. |

## Example operator configuration

```jsonc
{
  "storage": {
    "type": "sqlite+sqlcipher",
    "connectionstring": "sqlite+sqlcipher:///var/lib/fiskaltrust/service/queue.db",
    "sqlcipher": {
      "sqlcipherkeyalias": "fiskaltrust/middleware/{queueId}",
      "sqlcipherkeysource": "local-secure-element",
      "kdfIterations": 256000,
      "pageSize": 4096
    }
  }
}
```

The `connectionstring` selects the backend and file path.
The `sqlcipherkeyalias` tells the Launcher which local secret to fetch.
The Launcher resolves the key and injects it only into in-memory runtime configuration.

# Reference-level explanation

## Proposed architecture

This RFC introduces three concepts:

1. **A SQLCipher-aware SQLite storage mode**
   - A new storage variant or provider family for `sqlite+sqlcipher`.
   - It behaves like the current SQLite storage from the perspective of repositories and journals.
   - The only functional difference is how the connection is opened.

2. **A launcher-side key resolver**
   - A small abstraction that reads a wrapped or hardware-backed key from the local platform.
   - It returns key bytes or a key handle at startup.
   - The Launcher injects the resolved value into the in-memory `ftCashBoxConfiguration` / queue configuration object.

3. **A storage bootstrapper that uses the injected key**
   - The bootstrapper reads the SQLCipher key from configuration.
   - The connection factory opens the database using the SQLCipher provider.
   - Migrations run after the encrypted connection is established.

## Launcher flow

The Launcher already decrypts sensitive configuration values before runtime. The current Launcher implementation decrypts selected config keys in `ftCashBoxConfiguration` before a queue starts.
This RFC extends that model with a dedicated SQLCipher key field.

High-level flow:

1. Launcher reads its local configuration file.
2. Launcher resolves the SQLCipher key from the platform-specific local secret store.
3. Launcher injects the key into the queue configuration in memory only.
4. The queue package starts and uses that key to open SQLCipher.
5. The key is cleared from memory as soon as the connection is established, where feasible.

### Launcher snippet

```csharp
public interface ISqlCipherKeyProvider
{
    string ResolveKey(string keyAlias);
}

public sealed class LauncherConfigurationAugmentor
{
    private readonly ISqlCipherKeyProvider _keyProvider;

    public LauncherConfigurationAugmentor(ISqlCipherKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    public void ApplySqlCipherKey(ftCashBoxConfiguration cashboxConfiguration)
    {
        foreach (var queue in cashboxConfiguration.ftQueues)
        {
            if (!queue.Configuration.TryGetValue("sqlcipherkeyalias", out var aliasObj))
            {
                continue;
            }

            var keyAlias = aliasObj?.ToString();
            if (string.IsNullOrWhiteSpace(keyAlias))
            {
                continue;
            }

            var key = _keyProvider.ResolveKey(keyAlias);
            queue.Configuration["sqlcipherkey"] = key;
            queue.Configuration["connectionstring"] = queue.Configuration["connectionstring"]?.ToString();
        }
    }
}
```

Notes:
- The key is injected into the runtime object, not persisted back to disk.
- The `connectionstring` stays a normal launcher-managed config entry.
- A `sqlcipherkeyalias` indirection keeps the on-disk config free of key material.

## Storage-layer flow

The SQLite storage bootstrapper should obtain the SQLCipher key from configuration and pass it into the connection factory.
The existing repository model can remain unchanged.

### Storage snippet

```csharp
public sealed class SqlCipherStorageConfiguration
{
    [JsonProperty("connectionstring")]
    public string ConnectionString { get; set; }

    [JsonProperty("sqlcipherkey")]
    public string SqlCipherKey { get; set; }

    public int MigrationsTimeoutSec { get; set; } = 30 * 60;
}

public sealed class SqlCipherConnectionFactory
{
    public IDbConnection CreateConnection(string dbPath, string key)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Pooling = false,
            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
        };

        builder.Password = key;

        var connection = new Microsoft.Data.Sqlite.SqliteConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA cipher_compatibility = 4; PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();

        return connection;
    }
}
```

This preserves the repository code and changes only connection creation / bootstrap.

## Migration and compatibility

The migration strategy should be:

- Existing plain SQLite installations keep running unchanged.
- New installations can opt into `sqlite+sqlcipher`.
- Upgrading from plain SQLite to SQLCipher requires a one-time re-encryption / migration step.
- Once a database is encrypted, backups and restores must carry the same key alias or a rewrapped key.

A safe upgrade path is to ship a conversion command that:

1. Opens the old plain SQLite database.
2. Creates a new SQLCipher database.
3. Copies data row-by-row or via SQLCipher export/import.
4. Verifies row counts and checksum-like invariants.

# Drawbacks

- More complexity in launcher and storage initialization.
- Cross-platform secret management is not uniform.
- Recovery becomes harder if the local secure element or wrapped key is lost.
- Operators need explicit documentation for backup/restore and rotation.
- SQLCipher introduces a dependency on a provider/build that supports encrypted SQLite.

# Rationale and alternatives

## Why this design?

The proposal keeps the existing storage abstraction intact and localizes the change to two well-defined places:

- the Launcher, where secrets are available locally and can be injected at runtime;
- the SQLite bootstrapper, where the database connection is created.

That makes the feature implementable without reworking receipt logic or repository interfaces.

## Alternatives considered

### Plain SQLite + encrypted filesystem only
Rejected because filesystem encryption alone does not protect copied database files, backups, or exposed mounted volumes.

### Store the SQLCipher key in a plaintext config value
Rejected because it would defeat the purpose of database encryption.

### Use a remote KMS / cloud secret manager
Rejected for this RFC because the requirement is a local secure element / local secret store solution.
Remote secret managers may be useful later, but they are a different deployment model.

### Encode the key in the process command line
Rejected because process arguments are easy to expose in diagnostics and system tooling.

## Why BitLocker is not the whole Windows answer

BitLocker is excellent for protecting the disk.
It does not replace secure key handling for the SQLCipher key itself.
For Windows, the recommended design is BitLocker plus DPAPI/TPM protection for the wrapped SQLCipher key.

# Prior art

- SQLCipher is a common pattern for encrypted SQLite databases in mobile and desktop applications.
- Android Keystore and Secure Enclave-backed key storage are the standard local-secret patterns on their respective platforms.
- TPM2-bound secrets and `systemd-creds` are the closest Linux analogue to a local secure element.
- BitLocker is the standard Windows full-disk encryption technology, but should be paired with DPAPI or TPM-bound secret wrapping for application secrets.

# Unresolved questions

- Should the SQLCipher key be injected as a dedicated config field, an environment variable, or both?
- Should the Launcher support automatic key rotation, or should rotation be an explicit operator action?
- Should the first release support desktop keyrings on Linux, or require TPM2 for server-grade deployments?
- Should the SQLCipher provider support both `sqlite+sqlcipher://` and a plain `sqlite://` fallback, or should the storage type be a strict opt-in?
- Do we want the encryption key to be per-queue, per-machine, or per-cashbox?

# Future possibilities

- Add key rotation without re-provisioning the full cashbox.
- Add backup export/import tooling that bundles encrypted SQLite backups with key metadata references.
- Add metrics and health checks that report whether the SQLCipher key could be resolved from the local secure element.
- Extend the same secret-resolution abstraction to other sensitive launcher-managed values.
- Add a formal storage-provider interface that can support future encrypted backends beyond SQLite.
