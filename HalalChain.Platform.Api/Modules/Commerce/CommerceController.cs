using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Api.Modules.Events;
using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Contracts.Halal.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Commerce;

[ApiController]
[Route("api/v1/commerce")]
[Authorize(Roles = AuthConstants.RoleMarketplaceUser)]
[ApiVersion("1.0")]
public sealed class CommerceController(HalalChainDbContext db, IEventBus eventBus, ICurrentUser user) : ControllerBase
{
    private Guid CustomerId => user.RequireUserId();

    [HttpGet("cart")]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken ct)
    {
        var items = await db.CartItems
            .Where(c => c.CustomerId == CustomerId)
            .Include(c => c.Product).ThenInclude(p => p.Vendor)
            .Include(c => c.Product).ThenInclude(p => p.Certificates)
            .ToArrayAsync(ct);

        var currency = items.FirstOrDefault()?.Product?.Currency ?? "MYR";
        var dtos = items.Select(c => new CartItemDto(
            c.ProductId, c.Product!.Title, c.Product.Vendor?.Name ?? "",
            c.Quantity, c.Product.Price, c.Product.Currency,
            c.Product.Certificates.Any(c => c.Status == CertificateStatus.Verified && c.ExpiryDate > DateTimeOffset.UtcNow)
                ? "Verified" : "Unverified")).ToArray();
        var subtotal = items.Sum(c => c.Product!.Price * c.Quantity);

        return Ok(new CartDto(Guid.NewGuid(), dtos, subtotal, currency));
    }

    [HttpPost("cart/items")]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        if (request.Quantity < 1)
            return BadRequest(new ErrorResponse("INVALID_QUANTITY", "Quantity must be at least 1."));

        var product = await db.Products.FindAsync([request.ProductId], ct);
        if (product is null) return NotFound(new ErrorResponse("PRODUCT_NOT_FOUND", "Product not found."));

        if (product.Inventory < request.Quantity)
            return BadRequest(new ErrorResponse("INSUFFICIENT_INVENTORY", $"Only {product.Inventory} units available."));

        var existing = await db.CartItems
            .FirstOrDefaultAsync(c => c.CustomerId == CustomerId && c.ProductId == request.ProductId, ct);

        if (existing is not null)
            existing.Quantity += request.Quantity;
        else
            db.CartItems.Add(new CartItem { CustomerId = CustomerId, ProductId = request.ProductId, Quantity = request.Quantity });

        await db.SaveChangesAsync(ct);
        return await GetCart(ct);
    }

    [HttpPatch("cart/items")]
    public async Task<ActionResult<CartDto>> UpdateCartItem([FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.CustomerId == CustomerId && c.ProductId == request.ProductId, ct);

        if (item is not null)
        {
            if (request.Quantity <= 0) db.CartItems.Remove(item);
            else item.Quantity = request.Quantity;
            await db.SaveChangesAsync(ct);
        }
        return await GetCart(ct);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        // Idempotency: if IdempotencyKey is provided, check for existing order
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await db.Orders
                .FirstOrDefaultAsync(o => o.PaymentToken == request.IdempotencyKey, ct);
            if (existingOrder is not null)
                return await GetOrder(existingOrder.Id, ct);
        }

        var cartItems = await db.CartItems
            .Where(c => c.CustomerId == CustomerId)
            .Include(c => c.Product).ThenInclude(p => p!.Vendor)
            .ToArrayAsync(ct);

        if (cartItems.Length == 0)
            return BadRequest(new ErrorResponse("EMPTY_CART", "Cart is empty."));

        // Validate inventory
        foreach (var ci in cartItems)
        {
            if (ci.Product!.Inventory < ci.Quantity)
                return BadRequest(new ErrorResponse("INSUFFICIENT_INVENTORY",
                    $"Product '{ci.Product.Title}' only has {ci.Product.Inventory} units available."));
        }

        var order = new Order
        {
            CustomerId = CustomerId,
            Total = cartItems.Sum(c => c.Product!.Price * c.Quantity),
            Currency = cartItems.First().Product!.Currency,
            ShippingAddress = request.ShippingAddress,
            PaymentToken = request.IdempotencyKey,
        };
        db.Orders.Add(order);

        foreach (var group in cartItems.GroupBy(c => c.Product!.VendorId))
        {
            var vendorOrder = new VendorOrder
            {
                OrderId = order.Id,
                VendorId = group.Key,
                Subtotal = group.Sum(c => c.Product!.Price * c.Quantity),
                Currency = group.First().Product!.Currency,
            };
            db.VendorOrders.Add(vendorOrder);

            foreach (var ci in group)
            {
                db.OrderItems.Add(new OrderItem
                {
                    VendorOrderId = vendorOrder.Id,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product!.Price,
                    Currency = ci.Product.Currency,
                });
                ci.Product.Inventory -= ci.Quantity;
            }
        }

        db.CartItems.RemoveRange(cartItems);

        await eventBus.PublishAsync(new OrderPlacedEvent(order.Id, CustomerId, order.Total, order.Currency), ct);
        await db.SaveChangesAsync(ct);

        return await GetOrder(order.Id, ct);
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id, CancellationToken ct)
    {
        var o = await db.Orders
            .Include(o => o.VendorOrders).ThenInclude(vo => vo.Vendor)
            .Include(o => o.VendorOrders).ThenInclude(vo => vo.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (o is null)
            return NotFound(new ErrorResponse("ORDER_NOT_FOUND", $"Order {id} not found."));

        // Customer can only see their own orders
        if (o.CustomerId != CustomerId && !User.IsInRole(AuthConstants.RoleAdmin))
            return Forbid();

        return Ok(o.ToDto());
    }

    [HttpGet("orders")]
    public async Task<ActionResult<OrderDto[]>> ListOrders(CancellationToken ct)
    {
        var orders = await db.Orders
            .Where(o => o.CustomerId == CustomerId)
            .Include(o => o.VendorOrders).ThenInclude(vo => vo.Vendor)
            .Include(o => o.VendorOrders).ThenInclude(vo => vo.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToArrayAsync(ct);

        return Ok(orders.Select(o => o.ToDto()).ToArray());
    }
}

file static class OrderExtensions
{
    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.Status,
        o.VendorOrders.Select(vo => new VendorOrderDto(
            vo.Id, vo.VendorId, vo.Vendor?.Name ?? "", vo.Status,
            vo.Items.Select(i => new OrderItemDto(
                i.ProductId, i.Product?.Title ?? "", i.Quantity, i.UnitPrice, i.Currency)).ToArray(),
            vo.Subtotal, vo.Currency, vo.TrackingNumber)).ToArray(),
        o.Total, o.Currency, o.CreatedAt);
}
