namespace HalalChain.Domain.Catalog;

/// <summary>
/// Product lifecycle status. This is the projection of a VerdictBinding,
/// not an independent field. Only the state machine in the Application
/// layer may transition it.
/// </summary>
public enum ProductStatus
{
    Draft,
    PendingVerification,
    Active,
    ExpiringSoon,
    Suspended,
    Rejected,
    Archived,
}
