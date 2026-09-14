namespace HalalChain.Platform.Contracts.Vendors.Requests;

public sealed record RegisterVendorRequest(string Name, string? Country);

public sealed record UpdateVendorRequest(string? Name, string? Country, string? Status);
