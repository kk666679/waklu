namespace HalalChain.Platform.Contracts.Commerce.Dto;

public sealed record CartDto(
    Guid Id,
    CartItemDto[] Items,
    decimal Subtotal,
    string Currency);

public sealed record CartItemDto(
    Guid ProductId,
    string ProductTitle,
    string VendorName,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    string HalalStatus);

public sealed record OrderDto(
    Guid Id,
    string Status,
    VendorOrderDto[] VendorOrders,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record VendorOrderDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    string Status,
    OrderItemDto[] Items,
    decimal Subtotal,
    string Currency,
    string? TrackingNumber);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductTitle,
    int Quantity,
    decimal UnitPrice,
    string Currency);

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    int OrderCount,
    decimal TotalSpent,
    string Currency,
    DateTimeOffset JoinedAt);

public sealed record CreateOrderRequest(
    string FirstName,
    string LastName,
    string Email,
    string Address,
    string City,
    string PostalCode,
    CreateOrderItemRequest[] Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity);
