using HalalChain.Platform.Contracts.Catalog.Dto;
using HalalChain.Platform.Contracts.Commerce.Dto;
using HalalChain.Platform.Contracts.Commerce.Requests;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Http.Services;
using HalalChain.Realtime;
namespace HalalChain.Services;

public class AppState
{
    private readonly ILogger<AppState> _logger;

    public event Action? OnChange;
    public ProductDto[] Products { get; private set; } = [];
    public CategoryDto[] Categories { get; private set; } = [];
    public DepartmentDto[] Departments { get; private set; } = [];
    public VendorDto[] Vendors { get; private set; } = [];
    public OrderDto[] Orders { get; private set; } = [];
    public CustomerDto[] Customers { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    // Search / Filter state
    public string SearchQuery { get; set; } = "";
    public string? SelectedCategory { get; set; }
    public string? SelectedHalalStatus { get; set; }
    public ProductDto[] FilteredProducts => Products.Where(p =>
        (string.IsNullOrEmpty(SearchQuery) ||
         p.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
         (p.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
         p.VendorName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrEmpty(SelectedCategory) || p.CategoryName == SelectedCategory) &&
        (string.IsNullOrEmpty(SelectedHalalStatus) || p.HalalStatus?.Status == SelectedHalalStatus)
    ).ToArray();

    private readonly IPlatformApiClient _api;
    private readonly IRealtimeBroadcaster _realtime;
    private readonly Dictionary<Guid, List<ReviewDto>> _reviews = new();

    public AppState(IPlatformApiClient api, IRealtimeBroadcaster realtime, ILogger<AppState> logger)
    {
        _api = api;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task LoadProductsAsync(string? category = null)
    {
        IsLoading = true; Error = null; Notify();
        try
        {
            var result = await _api.GetProductsAsync(category);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("LoadProductsAsync failed: {Status} {Error}", result.Status, result.Error);
                Error = result.Error;
                Products = [];
            }
            else
            {
                Products = result.Data ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading products (category={Category})", category);
            Error = ex.Message;
            Products = [];
        }
        IsLoading = false; Notify();
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            var result = await _api.GetCategoriesAsync();
            if (!result.IsSuccess)
            {
                _logger.LogWarning("LoadCategoriesAsync failed: {Status} {Error}", result.Status, result.Error);
                Categories = [];
            }
            else
            {
                Categories = result.Data ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading categories");
            Categories = [];
        }
        Notify();
    }

    public async Task LoadVendorsAsync()
    {
        try
        {
            var result = await _api.GetVendorsAsync();
            if (!result.IsSuccess)
            {
                _logger.LogWarning("LoadVendorsAsync failed: {Status} {Error}", result.Status, result.Error);
                Vendors = [];
            }
            else
            {
                Vendors = result.Data ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading vendors");
            Vendors = [];
        }
        Notify();
    }

    public async Task LoadOrdersAsync()
    {
        IsLoading = true; Notify();
        try
        {
            var result = await _api.GetOrdersAsync();
            if (!result.IsSuccess)
            {
                _logger.LogWarning("LoadOrdersAsync failed: {Status} {Error}", result.Status, result.Error);
                Orders = [];
            }
            else
            {
                Orders = result.Data ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading orders");
            Orders = [];
        }
        IsLoading = false; Notify();
    }

    public async Task<OrderDto[]?> CreateOrderAsync(CheckoutRequest request)
    {
        try
        {
            var result = await _api.CheckoutAsync(request);
            if (result.IsSuccess && result.Data is not null)
            {
                var orders = result.Data;
                Orders = [.. Orders, orders];
                Notify();

                try
                {
                    var preview = orders.VendorOrders?.FirstOrDefault();
                    var vendorName = preview?.VendorName ?? "store";
                    var itemCount = preview?.Items?.Sum(i => i.Quantity) ?? 0;
                    await _realtime.SendNotificationToRoleAsync(
                        NotificationHub.Groups.Admin,
                        new RealtimeNotification(
                            "New order",
                            $"Order #{orders.Id.ToString()[..8].ToUpper()} from {vendorName} · {itemCount} item(s) · {orders.Currency} {orders.Total:N2}",
                            RealtimeNotificationKind.Info,
                            Topic: "orders.new"),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // Realtime is best-effort. The order itself is durable on the platform API.
                    _logger.LogWarning(ex, "Failed to broadcast new-order realtime notification");
                }
                return [orders];
            }
            _logger.LogWarning("CreateOrderAsync failed: {Status} {Error}", result.Status, result.Error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating order");
            return null;
        }
    }

    public async Task LoadCustomersAsync()
    {
        IsLoading = true; Notify();
        try
        {
            // No first-class customer list endpoint exists on the platform API
            // today. Surface that as an empty list and a debug log so the admin
            // Customers page renders an empty state rather than fabricating
            // data.
            _logger.LogDebug("LoadCustomersAsync: no canonical /commerce/customers endpoint; returning empty");
            Customers = [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in LoadCustomersAsync");
            Customers = [];
        }
        IsLoading = false; Notify();
    }

    public void SetSearchQuery(string query)
    {
        SearchQuery = query;
        Notify();
    }

    public void SetCategoryFilter(string? category)
    {
        SelectedCategory = category;
        Notify();
    }

    public void SetHalalFilter(string? status)
    {
        SelectedHalalStatus = status;
        Notify();
    }

    public Task<List<ReviewDto>?> GetReviewsAsync(Guid productId)
    {
        _reviews.TryGetValue(productId, out var reviews);
        return Task.FromResult(reviews);
    }

    public async Task SubmitReviewAsync(ReviewDto review)
    {
        if (!_reviews.ContainsKey(review.ProductId))
            _reviews[review.ProductId] = [];
        _reviews[review.ProductId].Add(review);
        await Task.CompletedTask;
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}
