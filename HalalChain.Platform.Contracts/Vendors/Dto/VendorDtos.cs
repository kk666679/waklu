namespace HalalChain.Platform.Contracts.Vendors.Dto;

public sealed record VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public FacilityDto[]? Facilities { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record FacilityDto(
    Guid Id,
    string Name,
    string? City,
    string? Country);
