using Nethereum.Web3.Accounts;

namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Supplies the hot wallet account used by the backend to send
/// transactions to the HalalChain platform contracts. In production
/// the key is loaded from a secret manager (Azure Key Vault, AWS SM,
/// HashiCorp Vault). In dev the key is loaded from configuration.
///
/// Implementations MUST:
///   - Never log the key
///   - Cache the key in memory (avoid round-tripping to the secret store on every tx)
///   - Rotate the key on a schedule (configurable; default 30 days)
/// </summary>
public interface IWalletProvider
{
    /// <summary>Returns the loaded <see cref="Account"/> (Nethereum's signing account).
    /// The same account is returned on every call; it's loaded once at startup.</summary>
    Account GetAccount();

    /// <summary>The wallet's checksum address. Equivalent to <c>GetAccount().Address</c>.</summary>
    string Address { get; }
}

/// <summary>
/// Default implementation that reads the private key from configuration
/// (via <c>Blockchain:HotWalletKey</c>) or, on Linux, from a file
/// pointed to by <c>Blockchain:HotWalletKeyFile</c> (mode 0600).
///
/// In production, replace this with an AzureKeyVaultWalletProvider that
/// reads from Key Vault and never touches the file system.
/// </summary>
public sealed class ConfigurationWalletProvider : IWalletProvider
{
    private readonly Account _account;

    public string Address => _account.Address;

    public ConfigurationWalletProvider(IConfiguration config, ILogger<ConfigurationWalletProvider> logger)
    {
        var key = config["Blockchain:HotWalletKey"];
        var keyFile = config["Blockchain:HotWalletKeyFile"];

        if (string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(keyFile))
        {
            if (!File.Exists(keyFile))
                throw new InvalidOperationException($"Hot wallet key file not found: {keyFile}");
            var fi = new FileInfo(keyFile);
            if (fi.Exists && (fi.UnixFileMode & UnixFileMode.UserRead) == 0)
                throw new InvalidOperationException("Hot wallet key file is not readable by the current user. Check file permissions.");
            key = File.ReadAllText(keyFile).Trim();
        }

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Neither Blockchain:HotWalletKey nor Blockchain:HotWalletKeyFile is configured.");

        if (key.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) key = key[2..];

        // Redact in logs — show only first 6 and last 4 chars
        var redacted = key.Length > 12 ? $"{key[..6]}…{key[^4..]}" : "***";
        logger.LogInformation("Hot wallet loaded. Address={Address} Key={Redacted}", /* address set below */ "", redacted);

        _account = new Account("0x" + key);
        logger.LogInformation("Hot wallet address resolved: {Address}", _account.Address);
    }

    public Account GetAccount() => _account;
}
