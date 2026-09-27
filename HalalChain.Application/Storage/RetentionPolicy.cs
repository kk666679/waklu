namespace HalalChain.Application.Storage;

public sealed record RetentionPolicy(
    TimeSpan Window,
    RetentionScope Scope);

public enum RetentionScope
{
    Certificate,
    Audit,
    AgentTrace,
    All,
}

public enum RetentionDecision
{
    Keep,
    Extend,
    Tombstone,
}

public sealed record RetentionReport(
    int Evaluated,
    int Kept,
    int Extended,
    int Tombstoned,
    DateTimeOffset CompletedAt);
