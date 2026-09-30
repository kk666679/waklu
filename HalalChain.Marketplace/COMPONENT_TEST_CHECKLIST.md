# Marketplace Component Test Checklist

This checklist guides manual testing of all Blazor components that have been updated to use the repository layer.

## Setup Before Testing

1. **Database**: PostgreSQL running with `halalchain_marketplace` database
2. **Connection**: Verify connection string in `appsettings.json`
3. **Migrations**: Run `dotnet run` once to apply all pending migrations
4. **Seed Data**: Components will work with empty database, but won't display much data

## Component Test Cases

### 1. Catalog Browse (`/browse`)

**Files**: 
- `Pages/Catalog/IndexViewModel.razor`
- `Models/ViewModels/CatalogViewModel.cs`
- `Repositories/IProductRepository.cs` + `ProductRepository.cs`

**Test Steps**:
- [ ] Navigate to `/browse`
- [ ] Verify page loads without errors
- [ ] Check "Loading products…" briefly shows, then disappears
- [ ] Verify "Product count" displays (should show 0 if no data)
- [ ] Test sorting dropdown (Relevance, Price Low→High, Price High→Low, Newest)
- [ ] Test filters:
  - [ ] Halal status checkboxes (Certified, Platform verified, Muslim-friendly)
  - [ ] Price range sliders (min/max)
  - [ ] Department dropdown (if departments exist)
- [ ] Click "Apply Filters" button
- [ ] Check "Reset" link clears all filters
- [ ] Verify pagination appears (if > 10 products)
- [ ] Click prev/next pagination buttons
- [ ] Check that URL reflects current page (`?page=2`)

**Expected Behavior**:
- ✓ No console errors
- ✓ Filters work and filter count updates
- ✓ Pagination works across pages
- ✓ Sorting dropdown changes product order
- ✓ Product cards display (if data exists)

**Failure Points**:
- ✗ "Connection string not found" → Check `appsettings.json`
- ✗ "Entity type not mapped" → Check `PlatformDbContext.OnModelCreating()`
- ✗ "NullReferenceException" → Check repository null checks
- ✗ Infinite loading → Check PostgreSQL connectivity

---

### 2. Product Card Component (`/browse` - within results)

**Files**:
- `Components/Shared/ProductCardViewModel.razor`
- `Models/ViewModels/ProductCardViewModel.cs`
- `Repositories/IProductRepository.cs`

**Test Steps**:
- [ ] Navigate to `/browse` (assumes products exist)
- [ ] Verify product card displays:
  - [ ] Product image (or placeholder)
  - [ ] Product name
  - [ ] Price formatted correctly
  - [ ] Halal badge (if certified)
  - [ ] Rating stars (if ratings exist)
  - [ ] Vendor name link
- [ ] Hover over card (should show slight highlight)
- [ ] Click on product name → Navigate to product detail
- [ ] Click "Add to cart" button:
  - [ ] Toast notification appears ("Added to cart")
  - [ ] Cart count in header increments
  - [ ] Button state changes (disabled briefly)
- [ ] Click wishlist heart icon:
  - [ ] Heart fills (toggles between filled/empty)
  - [ ] Toast appears
  - [ ] Wishlist count updates (if visible)

**Expected Behavior**:
- ✓ All product data displays correctly
- ✓ Interactions are responsive
- ✓ Toast notifications appear
- ✓ Links work correctly

**Failure Points**:
- ✗ Images don't load → Check `ProductMediaAsset` records in DB
- ✗ Halal badge missing → Check `HalalVerification` mapping
- ✗ "Add to cart" fails → Check `CartRepository` registration

---

### 3. Product Detail (`/products/{id}`)

**Files**:
- `Pages/Products/DetailViewModel.razor`
- `Models/ViewModels/ProductDetailViewModel.cs`
- `Repositories/IProductRepository.cs`, `ICertificateRepository.cs`

**Test Steps**:
- [ ] Navigate to `/products/{any-id}` (from catalog or direct URL)
- [ ] Verify page loads:
  - [ ] Product gallery displays (or placeholder)
  - [ ] Product name and description render
  - [ ] Price displays with currency formatting
  - [ ] Variants selector appears (if variants exist)
  - [ ] Halal certification section displays
  - [ ] Certificates with expiry dates shown
  - [ ] Verification status displayed
- [ ] Test gallery:
  - [ ] Click gallery thumbnails
  - [ ] Main image updates
  - [ ] Keyboard navigation (arrow keys)
- [ ] Test variants:
  - [ ] Select different sizes/colors
  - [ ] Price updates based on variant
  - [ ] Stock status updates
- [ ] Test add-to-cart:
  - [ ] Quantity selector works (+ and -)
  - [ ] Add to cart button works
  - [ ] Toast shows correct quantity
  - [ ] Cart header updates
- [ ] Test wishlist:
  - [ ] Heart icon toggles
  - [ ] Toast appears
- [ ] Scroll down:
  - [ ] Vendor information displays
  - [ ] Related products appear (if exists)
  - [ ] Reviews section loads

**Expected Behavior**:
- ✓ All product details load correctly
- ✓ Images display (or placeholders)
- ✓ Certificates and verification render
- ✓ All interactions responsive
- ✓ Price calculations correct

**Failure Points**:
- ✗ 404 page → Product doesn't exist in database
- ✗ "Cannot read property of null" → Check ProductDetailViewModel null checks
- ✗ Images broken → Check ProductMediaAsset records
- ✗ Certificates missing → Check HalalVerification and Certificate relationships

---

### 4. Halal Verification Display (`/verify`)

**Files**:
- `Pages/Verify/IndexViewModel.razor`
- `Models/ViewModels/VerificationViewModel.cs`
- `Repositories/IHalalVerificationRepository.cs`

**Test Steps**:
- [ ] Navigate to `/verify`
- [ ] Verify page loads
- [ ] Check verification list displays with:
  - [ ] Product name
  - [ ] Verification status badge (Approved, Pending, Rejected)
  - [ ] Compliance percentage
  - [ ] Policy version
  - [ ] Last updated date
- [ ] Test status badge colors:
  - [ ] Green for Approved
  - [ ] Yellow for Pending
  - [ ] Red for Rejected
- [ ] Test pagination:
  - [ ] Navigate through pages
  - [ ] Page count displays correctly
- [ ] Click on verification row:
  - [ ] Navigate to detail page (if implemented)
  - [ ] Evidence display shows:
    - [ ] Missing evidence items (if any)
    - [ ] Human review alerts (if pending)
    - [ ] Collected evidence list
- [ ] Test filters (if implemented):
  - [ ] Filter by status
  - [ ] Filter by policy
  - [ ] Search by product name

**Expected Behavior**:
- ✓ All verifications load
- ✓ Status badges display correctly
- ✓ Pagination works
- ✓ Evidence displays with proper formatting

**Failure Points**:
- ✗ No data shows → Check `HalalVerificationRepository` query
- ✗ Status badges missing → Check ViewModel mapping
- ✗ Evidence display empty → Check `VerificationEvidence` relationship

---

### 5. Vendor Marketplace (`/vendors`)

**Files**:
- `Pages/Vendors/IndexViewModel.razor`
- `Models/ViewModels/VendorListViewModel.cs`
- `Repositories/IVendorRepository.cs`

**Test Steps**:
- [ ] Navigate to `/vendors`
- [ ] Verify vendor list loads:
  - [ ] Vendor cards display with:
    - [ ] Vendor logo/image
    - [ ] Vendor name
    - [ ] Status badge (Active, Pending, Suspended)
    - [ ] Product count
    - [ ] Average rating
    - [ ] Location
- [ ] Test status badges:
  - [ ] Green for Active
  - [ ] Yellow for Pending
  - [ ] Gray for Suspended
- [ ] Test sorting (if available):
  - [ ] Sort by name
  - [ ] Sort by rating
  - [ ] Sort by product count
- [ ] Test pagination:
  - [ ] Navigate through pages
  - [ ] Vendor count updates
- [ ] Click vendor card:
  - [ ] Navigate to vendor detail/storefront
  - [ ] Show vendor's products

**Expected Behavior**:
- ✓ All vendors load with correct data
- ✓ Status badges display correctly
- ✓ Images load or show placeholders
- ✓ Pagination works

**Failure Points**:
- ✗ No vendors show → Check `VendorRepository` query
- ✗ Ratings missing → Check relationship with orders/reviews
- ✗ Status badges wrong → Check ViewModel mapping

---

### 6. Vendor Portal Dashboard (`/vendor/dashboard`)

**Files**:
- `Pages/VendorPortal/DashboardViewModel.razor`
- `Models/ViewModels/VendorDashboardViewModel.cs`
- `Repositories/IVendorRepository.cs`, `IOrderRepository.cs`

**Test Steps** (Requires vendor authentication):
- [ ] Log in as vendor user
- [ ] Navigate to `/vendor/dashboard`
- [ ] Verify dashboard loads:
  - [ ] Key metrics cards display:
    - [ ] Total products
    - [ ] Total orders
    - [ ] Total revenue
    - [ ] Average rating
- [ ] Check metrics are calculated correctly:
  - [ ] Product count from ProductRepository
  - [ ] Order count from OrderRepository
  - [ ] Revenue sum from VendorOrder.Total
  - [ ] Average rating from Product.Rating
- [ ] Verify "Top Performing Products" table:
  - [ ] Lists products with sales count
  - [ ] Sorts by sales descending
  - [ ] Shows top 5 products
  - [ ] Includes product name and image
- [ ] Check quick action buttons:
  - [ ] "Add Product" → Navigate to product form
  - [ ] "View Orders" → Navigate to orders page
  - [ ] "Analytics" → Navigate to analytics page
- [ ] Test refresh (if available):
  - [ ] Metrics update after adding a product
  - [ ] Cache invalidates properly

**Expected Behavior**:
- ✓ Dashboard loads for authenticated vendors
- ✓ All metrics display and calculate correctly
- ✓ Top products list accurate
- ✓ Quick action buttons work

**Failure Points**:
- ✗ 401 Unauthorized → Check authentication
- ✗ Metrics show 0 → Check repository queries
- ✗ Top products list empty → Check order/sales tracking
- ✗ NullReferenceException → Check null-safe navigation in ViewModel

---

### 7. Shopping Cart (`/cart`)

**Files**:
- `Pages/Cart/IndexViewModel.razor`
- `Models/ViewModels/CartViewModel.cs`
- `Repositories/ICartRepository.cs`, `IProductRepository.cs`

**Test Steps**:
- [ ] Add items to cart (from `/browse` or product detail)
- [ ] Navigate to `/cart`
- [ ] Verify cart loads:
  - [ ] All added items display
  - [ ] Product name, image, price display correctly
  - [ ] Quantity selector works (+/- buttons)
  - [ ] Subtotal calculates correctly (price × quantity)
  - [ ] Line total updates when quantity changes
- [ ] Test cart operations:
  - [ ] Remove item button works
  - [ ] Clear cart button removes all items
  - [ ] Update quantity button works
  - [ ] Cart persists after page reload
- [ ] Check cart summary:
  - [ ] Subtotal = sum of all line totals
  - [ ] Tax calculates (if applicable)
  - [ ] Shipping cost displays
  - [ ] Grand total = Subtotal + Tax + Shipping
- [ ] Test empty cart:
  - [ ] Remove all items
  - [ ] "Your cart is empty" message appears
  - [ ] "Continue shopping" link works
- [ ] Test checkout:
  - [ ] "Proceed to checkout" button enables (if items)
  - [ ] Click button → Navigate to checkout flow

**Expected Behavior**:
- ✓ All cart items display correctly
- ✓ Calculations are accurate
- ✓ Cart persists between page loads
- ✓ All operations responsive

**Failure Points**:
- ✗ Cart empty after reload → Check `CartRepository` persistence
- ✗ Calculations wrong → Check `LineTotal` property in ViewModel
- ✗ Items not removable → Check repository delete method
- ✗ "CartState not found" → Check DI registration in Program.cs

---

### 8. Order History (`/orders`)

**Files**:
- `Pages/Orders/IndexViewModel.razor`
- `Models/ViewModels/OrderListViewModel.cs`
- `Repositories/IOrderRepository.cs`

**Test Steps** (Requires existing orders):
- [ ] Navigate to `/orders`
- [ ] Verify orders load:
  - [ ] Order number/ID displays
  - [ ] Order date shows
  - [ ] Status badge displays (Pending, Processing, Shipped, Delivered)
  - [ ] Total amount shows
  - [ ] Item count shows
- [ ] Test status badge colors:
  - [ ] Yellow for Pending
  - [ ] Blue for Processing
  - [ ] Orange for Shipped
  - [ ] Green for Delivered
- [ ] Test sorting:
  - [ ] Sort by date (newest first)
  - [ ] Sort by status
  - [ ] Sort by total
- [ ] Test pagination:
  - [ ] Navigate through pages
  - [ ] Order count updates
- [ ] Click order row:
  - [ ] Navigate to order detail
  - [ ] Show items, tracking, invoice, etc.
- [ ] Test filters (if available):
  - [ ] Filter by status
  - [ ] Filter by date range

**Expected Behavior**:
- ✓ All orders display with correct data
- ✓ Status badges show correct colors
- ✓ Dates formatted consistently
- ✓ Pagination works

**Failure Points**:
- ✗ No orders show → Check `OrderRepository` query
- ✗ Status badges wrong → Check ViewModel mapping
- ✗ Totals incorrect → Check order calculation logic
- ✗ Pagination fails → Check query paging logic

---

## Automated Testing Summary

After manual testing, run automated tests:

```bash
# Run all Marketplace tests
dotnet test HalalChain.Marketplace.Tests -c Release

# Run specific test class
dotnet test HalalChain.Marketplace.Tests -c Release --filter "ClassName=ProductRepositoryTests"

# Run with verbose output
dotnet test HalalChain.Marketplace.Tests -c Release -v d
```

## Performance Testing

After all components pass functional tests:

1. **Load Time**: Measure `/browse` load time with 1000+ products
2. **Pagination**: Verify page transitions are smooth (< 500ms)
3. **Filtering**: Test filter performance with complex queries
4. **Search**: Test search performance (if implemented)
5. **Cart Operations**: Add/remove items performance (< 100ms)

## Sign-Off

- [ ] All components tested
- [ ] No critical errors found
- [ ] Performance acceptable
- [ ] Ready for Task 3 (Advanced pages)

**Date Tested**: ___________
**Tested By**: ___________
**Issues Found**: ___________
