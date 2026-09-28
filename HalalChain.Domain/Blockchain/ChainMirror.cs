namespace HalalChain.Domain.Blockchain;

/// <summary>
/// Local mirror of an on-chain record. The mirror is a cache; the chain
/// is authoritative. If they disagree, the chain wins.
///
/// This type carries no verdict field. The chain records existence,
/// revocation, and timestamps. Verdicts come from tawheed.
/// </summary>
public sealed record ChainMirror
{
    public required string TransactionHash { get; init; }
    public required long BlockNumber { get; init; }
    public required string ContractAddress { get; init; }
    public required string EventName { get; init; }
    public required string PayloadJson { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    public long? Confirmations { get; init; }
}
