namespace HalalChain.Domain.Common;

/// <summary>
/// Base for entities within an aggregate. Identity is by ID, not by value.
/// </summary>
public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected init; }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => Id.GetHashCode();
}
