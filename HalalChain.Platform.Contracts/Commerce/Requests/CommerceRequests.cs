using System.ComponentModel.DataAnnotations;

namespace HalalChain.Platform.Contracts.Commerce.Requests;

public sealed class AddToCartRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, 1000)]
    public int Quantity { get; init; }

    public AddToCartRequest() { }

    public AddToCartRequest(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}

public sealed class UpdateCartItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(0, 1000)]
    public int Quantity { get; init; }

    public UpdateCartItemRequest() { }

    public UpdateCartItemRequest(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}

public sealed class CheckoutRequest
{
    [MaxLength(500)]
    public string? ShippingAddress { get; init; }

    [MaxLength(200)]
    public string? PaymentToken { get; init; }

    [MaxLength(200)]
    public string? IdempotencyKey { get; init; }

    public CheckoutRequest() { }

    public CheckoutRequest(string? shippingAddress, string? paymentToken, string? idempotencyKey = null)
    {
        ShippingAddress = shippingAddress;
        PaymentToken = paymentToken;
        IdempotencyKey = idempotencyKey;
    }
}
