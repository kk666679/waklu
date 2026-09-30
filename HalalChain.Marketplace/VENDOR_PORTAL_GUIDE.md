# Vendor Portal Guide

This document describes the Vendor Portal feature in HalalChain.Marketplace, a complete vendor management interface with dashboard, product management, analytics, and order fulfillment.

## Feature Overview

The Vendor Portal provides:
- **Dashboard** (`/vendor/dashboard`) - Overview of key metrics and performance
- **Product Management** (`/vendor/products`) - Manage product catalog
- **Analytics** (`/vendor/analytics`) - Performance metrics and insights
- **Order Management** (`/vendor/orders`) - Handle customer orders and fulfillment
- **Authentication** - Vendor-only access with authentication checks

## Pages

### 1. Vendor Dashboard (`/vendor/dashboard`)

**Route**: `/vendor/dashboard`  
**Component**: `Pages/VendorPortal/DashboardViewModel.razor`  
**Access**: Vendor authentication required  

#### Features:
- Welcome message with vendor name
- **Key Metrics**:
  - Active Products (total count)
  - Total Orders (all-time)
  - Pending Orders (requires attention)
  - Monthly Revenue (last 30 days)
- **Top Performing Products** table:
  - Product name with link to detail
  - Revenue generated
  - Units sold
  - Top 5 products by revenue
- **Quick Action Buttons**:
  - Manage Products
  - View Orders
  - View Analytics
  - Settings

#### Data Flow:
```
VendorRepository.GetByIdAsync() - Get vendor profile
ProductRepository.GetByVendorAsync() - Get vendor's products
OrderRepository.GetByStatusAsync() - Get pending orders
OrderRepository.GetAllAsync() - Get all orders
OrderRepository.GetRevenueByDateRangeAsync() - Calculate monthly revenue
CatalogAnalyticsService.GetTopPerformersAsync() - Get top products
```

#### Authentication:
- Checks CurrentUserAccessor.GetIdAsync() to verify vendor is authenticated
- Shows "Sign in required" message if not authenticated
- Shows "Vendor account not found" if vendor profile doesn't exist

---

### 2. Product Management (`/vendor/products`)

**Route**: `/vendor/products`  
**Component**: `Pages/VendorPortal/ProductsViewModel.razor`  

#### Features:
- **Filters & Search**:
  - Search by product name or SKU
  - Filter by status (Active, Inactive, Draft)
  - Filter by halal status (Certified, Verified, Pending)
  - Clear filters button
- **Product Table**:
  - Product name and SKU
  - Price with currency
  - Stock level (color-coded if out of stock)
  - Halal status badge
  - Product status badge
  - Actions: Edit, View, Delete
- **Pagination**: 20 products per page
- **Empty State**: "No products yet" with Create button

#### Data Flow:
```
ProductRepository.GetByVendorAsync(vendorId, skip, take)
  ↓ (with pagination)
ProductViewModel[] (mapped via AutoMapper)
  ↓
ProductsViewModel.razor (renders table with filters)
```

#### Actions:
- **Edit**: Navigate to `/vendor/products/{id}/edit` (TODO: Create edit form)
- **View**: Navigate to `/products/{id}` (public product page)
- **Delete**: Remove product from catalog (requires confirmation)

#### Filtering:
- Real-time search updates product count
- Status filter dropdown with all options
- Halal status filter for compliance tracking

---

### 3. Analytics Dashboard (`/vendor/analytics`)

**Route**: `/vendor/analytics`  
**Component**: `Pages/VendorPortal/AnalyticsViewModel.razor`  

#### Features:

**Time Period Selector**:
- Last 7 days
- Last 30 days (default)
- Last 90 days
- Last year

**Key Metrics** (with comparisons):
- Total Revenue (with % change vs previous period)
- Total Orders (with count change)
- Average Order Value
- Product Views (with conversion rate)
- Pending/Processing/Shipped/Delivered/Cancelled order counts

**Top Products by Revenue**:
- Product name
- Revenue generated
- Units sold
- Conversion rate

**Top Categories**:
- Category name
- Category sales (mock data placeholder)

**Order Status Distribution**:
- Pending orders
- Processing orders
- Shipped orders
- Delivered orders
- Cancelled orders

**Recent Orders Table**:
- Order number (clickable)
- Item count
- Order total amount
- Status badge (color-coded)
- Order date
- Last 10 orders shown

#### Data Flow:
```
OrderRepository.GetByDateRangeAsync(startDate, endDate)
  ↓
Analytics calculations (sum, count, average)
  ↓
CatalogAnalyticsService.GetTopPerformersAsync()
  ↓
AnalyticsViewModel.razor (renders metrics & charts)
```

#### Metrics Calculated:
- Revenue: Sum of order totals within period
- Orders: Count of orders within period
- Avg Order Value: Revenue / Orders
- Order Status Breakdown: Counts by status
- Top Products: By revenue sum

---

### 4. Order Management (`/vendor/orders`)

**Route**: `/vendor/orders`  
**Component**: `Pages/VendorPortal/OrdersViewModel.razor`  

#### Features:

**Filters & Search**:
- Search by order number
- Filter by status (Pending, Processing, Shipped, Delivered, Cancelled)
- Clear filters button

**Orders Table**:
- Order number (clickable link to detail)
- Customer name and email
- Item count in order
- Order total amount
- Status badge (color-coded)
- Order date (formatted as "MMM dd, yyyy")
- Actions: View, Mark as shipped

**Pagination**: 20 orders per page

**Status-Based Actions**:
- For Pending/Processing orders: "Mark as shipped" button (📦)
- For completed orders: View-only mode

#### Data Flow:
```
OrderRepository.GetByStatusAsync(status, skip, take)
  OR
OrderRepository.GetAllAsync(skip, take)
  ↓
OrderViewModel[] (mapped from Order entities)
  ↓
OrdersViewModel.razor (renders table with filters)
```

#### Order Details (TODO - Detail Page):
- Navigate to `/vendor/orders/{id}` to view full order details
- Order items with quantities and prices
- Customer shipping address
- Order timeline/status history
- Option to update order status

---

## ViewModels

### ProductViewModel
```csharp
public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } // MYR
    public int Inventory { get; set; }
    public bool IsOutOfStock { get; }
    public string? HalalStatus { get; set; }
    public List<ProductMediaViewModel> MediaAssets { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### VendorViewModel
```csharp
public class VendorViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Repository Methods Used

### IProductRepository
```csharp
Task<IEnumerable<Product>> GetByVendorAsync(Guid vendorId, int skip = 0, int take = 50);
Task<Product?> GetByIdAsync(Guid id);
```

### IOrderRepository
```csharp
Task<IEnumerable<Order>> GetByStatusAsync(string status, int skip = 0, int take = 50);
Task<IEnumerable<Order>> GetAllAsync(int skip = 0, int take = 50);
Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);
```

### IVendorRepository
```csharp
Task<Vendor?> GetByIdAsync(Guid id);
```

### ICatalogAnalyticsService
```csharp
Task<List<ProductPerformance>> GetTopPerformersAsync(Guid vendorId, string sortBy, int take);
```

## Authentication & Authorization

All vendor portal pages require authentication:

1. **Check Current User**:
   ```csharp
   VendorId = await CurrentUser.GetIdAsync();
   _isAuthenticated = VendorId != Guid.Empty;
   ```

2. **Verify Vendor Exists**:
   ```csharp
   var vendor = await VendorRepo.GetByIdAsync(VendorId);
   if (vendor == null) { /* show error */ }
   ```

3. **Return URLs**: For login flow, add `?returnUrl=/vendor/dashboard` to login link

## Status Badges

### Product Status
- ✓ Active (Green) - Product is live and selling
- ⚠ Out of stock (Yellow) - No inventory but not delisted

### Halal Status
- ✓ Certified (Green) - Has valid halal certification
- ⓘ Verified (Blue) - Platform verified as halal-compliant
- ⚠ Pending (Yellow) - Awaiting verification
- - Not set (Gray) - No halal status assigned yet

### Order Status
- ⚠ Pending (Yellow) - Needs vendor action
- ⓘ Processing (Blue) - Being prepared for shipment
- 📦 Shipped (Purple) - In transit
- ✓ Delivered (Green) - Delivered to customer
- ✗ Cancelled (Red) - Order cancelled

## Future Enhancements

1. **Product Management**
   - Create new product form
   - Edit product details
   - Bulk import/export products
   - Product duplicate/clone feature

2. **Order Fulfillment**
   - Order detail page with timeline
   - Integration with shipping providers
   - Tracking number management
   - Bulk order status updates

3. **Advanced Analytics**
   - Charts and graphs for trends
   - Customer insights and retention
   - Seasonal trends
   - Competitor benchmarking

4. **Customer Management**
   - View customer profiles
   - Customer communication history
   - Repeat customer analytics
   - Customer ratings & reviews

5. **Settings**
   - Profile management
   - Bank account information
   - Tax settings
   - Notification preferences
   - API integration settings

## Performance Considerations

### Query Optimization
- Use eager loading (.Include()) for related data
- Implement pagination (20 items per page)
- Cache top performers and metrics

### Database Indexes
- Index on `Products.VendorId` for vendor product queries
- Index on `Orders.CreatedAt` for date range queries
- Index on `Orders.Status` for status filtering

### Caching Strategy
- Cache dashboard metrics (1-hour TTL)
- Cache top products (4-hour TTL)
- Invalidate on order/product updates

## Testing

### Test Cases

1. **Dashboard Access**
   - [ ] Sign out → Navigate to dashboard → Redirect to login
   - [ ] Sign in → Navigate to dashboard → Loads metrics
   - [ ] Verify all 4 metric cards display
   - [ ] Verify top products table shows data

2. **Product Management**
   - [ ] Search products by name
   - [ ] Search products by SKU
   - [ ] Filter by status
   - [ ] Filter by halal status
   - [ ] Test pagination
   - [ ] Click edit → navigate to edit form
   - [ ] Click view → navigate to public page
   - [ ] Click delete → confirm dialog → product removed

3. **Analytics**
   - [ ] Select different time periods
   - [ ] Verify metrics recalculate
   - [ ] Check top products change by period
   - [ ] Verify order status breakdown updates
   - [ ] Test recent orders table pagination

4. **Order Management**
   - [ ] Filter by order status
   - [ ] Search by order number
   - [ ] Test pagination
   - [ ] Click view order → navigate to detail
   - [ ] Click "mark as shipped" → status updates

## Related Features

- [Product Detail Page](./PRODUCT_DETAIL.md)
- [Verification Feature](./VERIFICATION_FEATURE.md)
- [Testing Guide](./TESTING_GUIDE.md)
- [Component Migration Guide](./COMPONENT_MIGRATION_GUIDE.md)
