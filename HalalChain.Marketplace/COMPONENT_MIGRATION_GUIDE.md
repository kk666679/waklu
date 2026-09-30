# Blazor Component Migration Guide: Domain → ViewModel

## Overview
This guide explains how to update Blazor components to use the new repository layer and ViewModels instead of directly consuming DTOs from the API layer.

---

## Architecture Changes

### Before (Current)
```
Blazor Component
    ↓
IPlatformApiClient (HTTP)
    ↓
API Layer
    ↓
Data Transfer Objects (DTO)
    ↓
Component re-renders with DTO
```

### After (New)
```
Blazor Component
    ↓
Repository (IProductRepository, IOrderRepository, etc.)
    ↓
EF Core Query against PlatformDbContext
    ↓
Domain Model (Product, Order, Certificate, etc.)
    ↓
AutoMapper (Domain → ViewModel)
    ↓
ViewModel (ProductViewModel, OrderViewModel, etc.)
    ↓
Component re-renders with strongly-typed ViewModel
```

---

## Key Benefits

1. **Type Safety**: ViewModels are strongly-typed DTOs designed specifically for Blazor binding
2. **Computed Properties**: ViewModels include derived values (LineTotal, IsExpired, DisplayStatus, etc.)
3. **No Circular References**: ViewModels avoid navigation loops safe for serialization
4. **Local-First**: Queries run against local PostgreSQL database, not remote API
5. **AutoMapper**: Automatic domain→ViewModel projection
6. **Repository Pattern**: Clean data access abstractions

---

## Migration Steps

### Step 1: Inject Repositories Instead of API Clients

**Before:**
```csharp
@inject IPlatformApiClient Api
@inject CartState Cart
```

**After:**
```csharp
@inject IProductRepository ProductRepo
@inject ICartRepository CartRepo
@inject IMapper Mapper
@inject INotificationService Toast
@inject ILogger<ProductPage> Logger
```

### Step 2: Load Data from Repository

**Before:**
```csharp
protected override async Task OnInitializedAsync()
{
    var result = await Api.GetProductAsync(Id);
    Product = result.Data;
}
```

**After:**
```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        var product = await ProductRepo.GetByIdAsync(Id);
        if (product == null)
        {
            // Product not found
            return;
        }
        ProductModel = Mapper.Map<ProductViewModel>(product);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error loading product {ProductId}", Id);
        Toast.Error("Failed to load product");
    }
}
```

### Step 3: Update @bind Directives

**Before (DTO with nullable properties):**
```html
<img src="@Product.ImageUrl" alt="@Product.Title" />
<p>@Product.Price.ToString("N2")</p>
```

**After (ViewModel with safe null-coalescing):**
```html
<img src="@ProductModel.MediaAssets?.FirstOrDefault()?.Url" alt="@ProductModel.Title" />
<p>@ProductModel.Currency @ProductModel.Price.ToString("N2")</p>
```

### Step 4: Use ViewModel Computed Properties

**Before:**
```csharp
bool IsExpired => product.ExpiryDate < DateTime.UtcNow;
string DisplayStatus => product.Status switch { ... };
```

**After (Already in ViewModel):**
```html
<span class="@CertModel.StatusBadgeClass">@CertModel.DisplayStatus</span>
```

### Step 5: Update Event Handlers

**Before:**
```csharp
private async Task AddToCart()
{
    var ok = await Cart.AddAsync(Product.Id);
}
```

**After:**
```csharp
private async Task AddToCart()
{
    try
    {
        var cartItem = new CartItem 
        { 
            Id = Guid.NewGuid(),
            CustomerId = CurrentUserId,
            ProductId = ProductModel.Id,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow
        };
        await CartRepo.AddAsync(cartItem);
        Toast.Success($"{ProductModel.Title} added to cart");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error adding to cart");
        Toast.Error("Could not add to cart");
    }
}
```

---

## Component-by-Component Migration

### ProductCard Component
**Change:**
- Parameter: `EnrichedProductDto` → `ProductSummaryViewModel`
- Remove API calls (card is stateless display)
- Use ViewModel computed properties (HalalStatus, VendorName, etc.)

**File:** `Components/Shared/ProductCard.razor`

```csharp
@code {
    [Parameter, EditorRequired] 
    public ProductSummaryViewModel Product { get; set; } = default!;
    
    [Inject] 
    public ICartRepository CartRepo { get; set; } = default!;
    
    [Inject] 
    public IWishlistRepository WishlistRepo { get; set; } = default!;
    
    [Inject] 
    public IMapper Mapper { get; set; } = default!;
}
```

### Cart/Index Page
**Change:**
- Inject `ICartRepository` instead of `CartState`
- Load cart items via repository query
- Map to `CartSummaryViewModel`
- Handle CRUD via repository methods

### Catalog/Index Page
**Change:**
- Inject `IProductRepository`, `IDepartmentRepository`, `ISubcategoryRepository`
- Load products/departments/filters via repositories
- Map to `ProductSummaryViewModel[]`, `DepartmentViewModel`, etc.
- Build query filters locally (no API call)

### Order/Index Page
**Change:**
- Inject `IOrderRepository`
- Load orders via repository with status filter
- Map to `OrderHistoryViewModel[]`
- Display via component binding

---

## Important Considerations

### 1. Authentication/Authorization
Continue using `AuthService` and `ICurrentUserAccessor` to get current user context:
```csharp
[Inject] public ICurrentUserAccessor CurrentUser { get; set; } = default!;

var userId = await CurrentUser.GetIdAsync();
var orders = await OrderRepo.GetByCustomerAsync(userId);
```

### 2. Loading States
Add loading indicators during async operations:
```csharp
private bool _isLoading = true;

protected override async Task OnInitializedAsync()
{
    try
    {
        // Load data
    }
    finally
    {
        _isLoading = false;
    }
}
```

### 3. Error Handling
Always wrap repository calls in try-catch and log/notify user:
```csharp
try 
{ 
    await repo.MethodAsync(); 
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    await Toast.ErrorAsync("Operation failed. Please try again.");
}
```

### 4. StateHasChanged
Call `StateHasChanged()` or `InvokeAsync(StateHasChanged)` after updates:
```csharp
await CartRepo.UpdateAsync(item);
await InvokeAsync(StateHasChanged);
```

### 5. Disposal for Real-Time Updates
If using SignalR or event-based updates, implement `IDisposable`:
```csharp
public void Dispose()
{
    Cart.OnChange -= OnCartChanged;
}
```

---

## Common ViewModel Usage Patterns

### Display Halal Status Badge
```html
<span class="@HalalModel.StatusBadgeClass">
    @HalalModel.DisplayStatus
</span>
```

### Format Currency
```html
<span>@Product.Currency @Product.Price.ToString("N2")</span>
```

### Check Product Availability
```html
@if (Product.IsOutOfStock)
{
    <button disabled>Out of stock</button>
}
else
{
    <button @onclick="AddToCart">Add to cart</button>
}
```

### Calculate Order Summary
```html
<div>Items: @Cart.ItemCount</div>
<div>Subtotal: @Cart.Currency @Cart.SubTotal.ToString("N2")</div>
<div>Tax: @Cart.Currency @Cart.TaxAmount.ToString("N2")</div>
<div>Total: @Cart.Currency @Cart.Total.ToString("N2")</div>
```

### Handle Expiring Certificate
```html
@if (Certificate.IsExpired)
{
    <span class="badge bg-danger">Expired</span>
}
else if (Certificate.IsExpiringSoon)
{
    <span class="badge bg-warning">Expiring in @Certificate.DaysUntilExpiry days</span>
}
```

---

## Testing Checklist

- [ ] Components load data from repositories
- [ ] AutoMapper successfully projects domain → ViewModels
- [ ] ViewModels display correctly in UI
- [ ] Computed properties work (LineTotal, IsExpired, etc.)
- [ ] Add/Update/Delete operations work via repositories
- [ ] Error handling shows toast notifications
- [ ] Loading states display while querying
- [ ] No circular reference warnings
- [ ] CurrentUser context flows through properly
- [ ] Navigation works after state changes

---

## File Updates Required

### Core Components/Pages
1. `Components/Shared/ProductCard.razor` - Use ProductSummaryViewModel
2. `Components/Shared/VendorCard.razor` - Use VendorViewModel
3. `Components/Shared/HalalStatusBadge.razor` - Accept HalalVerificationViewModel
4. `Pages/Catalog/Index.razor` - Load departments/products from repos
5. `Pages/Cart/Index.razor` - Load cart from CartRepository
6. `Pages/Orders/Index.razor` - Load orders from OrderRepository
7. `Pages/Products/Detail.razor` - Load product from ProductRepository
8. `Pages/Verify/Index.razor` - Load verifications from HalalVerificationRepository

### Optional but Recommended
- `Pages/Home/Index.razor` - Featured products from ProductRepository
- `Pages/VendorPortal/*.razor` - Vendor analytics from repositories
- `Pages/Admin/*.razor` - Admin pages use repositories

---

## Migration Priority

**Phase 1 (Critical):**
- ProductCard component
- Cart Index page
- Catalog Index page

**Phase 2 (High):**
- Order pages
- Product detail page
- Verification pages

**Phase 3 (Medium):**
- Vendor pages
- Admin pages
- Dashboard pages

---

## Questions/Issues?

Refer to:
- `Models/ViewModels/*.cs` for ViewModel structure
- `Repositories/*.cs` for available query methods
- `Models/Mapping/MappingProfile.cs` for mapping configuration
