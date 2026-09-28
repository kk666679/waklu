namespace HalalChain.Domain.Vendors;

using HalalChain.Domain.Common;

/// <summary>
/// A vendor. Owns the storefront, is the target of certificates, and is
/// the entity whose compliance state determines whether its products can
/// be listed.
/// </summary>
public sealed class Vendor : AggregateRoot<VendorId>
{
    public string LegalName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string CountryCode { get; private set; } = "MY";
    public VendorStatus Status { get; private set; } = VendorStatus.Pending;
    public DateTimeOffset RegisteredAt { get; private set; }

    private Vendor() { }

    public static Vendor Register(
        string legalName,
        string displayName,
        string countryCode,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var vendor = new Vendor
        {
            Id = VendorId.New(),
            LegalName = legalName,
            DisplayName = displayName,
            CountryCode = countryCode,
            Status = VendorStatus.Pending,
            RegisteredAt = now,
        };

        vendor.Raise(new VendorRegistered(vendor.Id, legalName, now));
        return vendor;
    }

    public void Activate(DateTimeOffset now)
    {
        if (Status == VendorStatus.Active) return;
        Status = VendorStatus.Active;
        Raise(new VendorActivated(Id, now));
    }

    public void Suspend(string reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = VendorStatus.Suspended;
        Raise(new VendorSuspended(Id, reason, now));
    }
}

public enum VendorStatus
{
    Pending,
    Active,
    Suspended,
    Closed,
}
