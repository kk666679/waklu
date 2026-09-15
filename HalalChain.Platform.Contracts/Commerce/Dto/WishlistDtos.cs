using HalalChain.Platform.Contracts.Catalog.Dto;

namespace HalalChain.Platform.Contracts.Commerce.Dto;

public sealed record WishlistDto(
    Guid CustomerId,
    WishlistItemDto[] Items);

public sealed record WishlistItemDto(
    Guid ProductId,
    string ProductTitle,
    string VendorName,
    decimal Price,
    string Currency,
    HalalStatusDto HalalStatus,
    DateTimeOffset AddedAt);
