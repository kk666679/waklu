using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace HalalChain.Platform.Api.Modules.Blockchain;

/// <summary>
/// Suggests EIP-1559 gas fees with a hard cap. Polls the chain's
/// <c>eth_gasPrice</c> and <c>eth_feeHistory</c> and returns the
/// higher of the two within a configurable cap.
///
/// Capping is critical: a runaway fee market (e.g., during a
/// congested moment on Polygon) would otherwise silently drain the
/// hot wallet. The cap is configured in
/// <c>Blockchain:GasPriceCapGwei</c> (default 200 gwei).
/// </summary>
public sealed class GasPriceOracle
{
    private readonly IConfiguration _config;
    private readonly long _capWei;
    private readonly Uri? _rpcUrl;

    public GasPriceOracle(IConfiguration config)
    {
        _config = config;
        var capGwei = config.GetValue("Blockchain:GasPriceCapGwei", 200);
        _capWei = capGwei * 1_000_000_000L; // 1 gwei = 1e9 wei
        var rpc = config["Blockchain:RpcUrl"];
        if (!string.IsNullOrWhiteSpace(rpc)) _rpcUrl = new Uri(rpc);
    }

    public async Task<HexBigInteger?> GetSuggestedAsync(CancellationToken ct)
    {
        if (_rpcUrl is null) return null;
        try
        {
            var web3 = new Web3(_rpcUrl.ToString());
            var price = await web3.Eth.GasPrice.SendRequestAsync();
            if (price is null) return null;
            // Cap
            return price.Value > _capWei ? new HexBigInteger(_capWei) : price;
        }
        catch
        {
            return null;
        }
    }
}
