namespace HalalChain.Domain;

/// <summary>
/// Marker for aggregate roots. Only aggregate roots have repositories.
/// Enforced by convention and by HalalChain.Architecture.Tests.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
