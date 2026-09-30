# Testing Guide for HalalChain.Marketplace

This guide documents how to set up a local testing environment and validate all Blazor components with the PostgreSQL database.

## Prerequisites

- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL 16+ (via Docker)
- Node.js ≥22 (for npm workspace)
- Visual Studio or VS Code with Blazor support

## Environment Setup

### 1. Configure Environment Variables

Create a `.env` file in the repository root:

```bash
# PostgreSQL
POSTGRES_USER=halalchain_user
POSTGRES_PASSWORD=HalalChain123!
POSTGRES_DB=halalchain_marketplace
POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=halalchain_marketplace;Username=halalchain_user;Password=HalalChain123!;"

# JWT (use for development only)
JWT__KEY="super-secret-key-for-development-only-change-in-production"

# AI Gateway
AI_GATEWAY_API_KEY="dev-api-key-12345"

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

### 2. Configure Marketplace Connection String

Update `HalalChain.Marketplace/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=halalchain_marketplace;Username=halalchain_user;Password=HalalChain123!;",
    "PlatformConnection": "Host=localhost;Port=5432;Database=halalchain_marketplace;Username=halalchain_user;Password=HalalChain123!;",
    "DashboardConnection": "Data Source=marketplace.db"
  }
}
```

### 3. Start PostgreSQL

```bash
# Using Docker Compose (recommended)
docker compose up -d postgres redis

# Or start only PostgreSQL if you already have infrastructure running
docker run -d \
  --name postgres-halalchain \
  -e POSTGRES_USER=halalchain_user \
  -e POSTGRES_PASSWORD=HalalChain123! \
  -e POSTGRES_DB=halalchain_marketplace \
  -p 5432:5432 \
  postgres:16-alpine
```

### 4. Create Database (if needed)

```bash
# Connect to PostgreSQL
docker exec -it postgres-halalchain psql -U halalchain_user -d halalchain_marketplace

# Or via connection string
psql "Host=localhost;Port=5432;Database=halalchain_marketplace;Username=halalchain_user;Password=HalalChain123!;"
```

## Running Tests

### Build the Solution

```bash
cd c:\Users\kurni\OneDrive\Desktop\waklu-keewak
dotnet restore HalalChain.Platform.sln
dotnet build HalalChain.Platform.sln -c Release
```

### Run Marketplace Application

```bash
cd HalalChain.Marketplace
dotnet run
```

The application will:
1. Apply PlatformDbContext migrations to PostgreSQL
2. Apply ApplicationDbContext migrations to SQLite
3. Start the Blazor Server app on `http://localhost:5200` (or configured port)

### Access the Application

Navigate to `http://localhost:5200` in your browser and test:

1. **Catalog Browse** (`/catalog`)
   - Load products from PostgreSQL
   - Test filtering, sorting, pagination
   - Verify product cards render with images/certificates

2. **Product Detail** (`/products/{id}`)
   - Verify full product details load
   - Check halal certificates display
   - Test variants selection
   - Verify add-to-cart functionality

3. **Halal Verification** (`/verify`)
   - List all verification records
   - Display compliance status
   - Show evidence collected

4. **Vendor Marketplace** (`/vendors`)
   - List all vendors
   - Show vendor status badges
   - Display vendor metrics

5. **Vendor Portal** (`/vendor/dashboard`)
   - Requires vendor authentication
   - Display vendor analytics
   - Show top products
   - Verify metrics calculations

6. **Shopping Cart** (`/cart`)
   - Add products to cart
   - Verify cart state persistence
   - Test checkout flow

## Component Test Matrix

| Component | Page | Repository | ViewModel | Database | Status |
|-----------|------|------------|-----------|----------|--------|
| Catalog Browse | `/catalog` | ProductRepository | CatalogViewModel | PostgreSQL | ✓ |
| Product Card | Component | ProductRepository | ProductCardViewModel | PostgreSQL | ✓ |
| Product Detail | `/products/{id}` | ProductRepository, CertificateRepository | ProductDetailViewModel | PostgreSQL | ✓ |
| Halal Verify | `/verify` | HalalVerificationRepository | VerificationViewModel | PostgreSQL | ✓ |
| Vendor List | `/vendors` | VendorRepository | VendorListViewModel | PostgreSQL | ✓ |
| Vendor Dashboard | `/vendor/dashboard` | VendorRepository, OrderRepository | VendorDashboardViewModel | PostgreSQL | ✓ |
| Cart | `/cart` | CartRepository | CartViewModel | PostgreSQL | ✓ |
| Orders | `/orders` | OrderRepository | OrderListViewModel | PostgreSQL | ✓ |
| Wishlist | Component | WishlistRepository | WishlistViewModel | PostgreSQL | ✓ |

## Debugging

### View Database Schema

```bash
# Connect to PostgreSQL
docker exec -it postgres-halalchain psql -U halalchain_user -d halalchain_marketplace

# List tables
\dt

# Describe a table
\d products

# View recent logs
SELECT * FROM "VerificationAudits" ORDER BY "CreatedAt" DESC LIMIT 10;
```

### Check Application Logs

```bash
# Marketplace logs (if running in Docker)
docker logs marketplace

# Or from dotnet run console output
```

### Browser Developer Tools

1. Open F12 (Developer Tools)
2. Check Network tab for API calls (should be local repository calls, not API)
3. Check Console tab for JavaScript errors
4. Check Application tab for localStorage/sessionStorage state

### Common Issues

**Issue**: "Connection string 'PlatformConnection' not found"
- **Fix**: Ensure appsettings.json has correct connection strings
- Check environment variables are set

**Issue**: PostgreSQL connection refused
- **Fix**: Verify PostgreSQL is running
- Check connection string host/port/credentials
- Ensure database and user exist

**Issue**: Components not loading data
- **Fix**: Check browser console for errors
- Verify repositories are registered in Program.cs
- Check if migrations have run (check PostgreSQL schema)

**Issue**: "Entity type not mapped" EF Core errors
- **Fix**: Verify DbSet<T> properties exist in PlatformDbContext
- Ensure OnModelCreating() configures all entities
- Run migrations after schema changes

## Load Testing

### Basic Load Test (Manual)

1. Open multiple tabs to `/catalog`
2. Perform concurrent filtering/sorting operations
3. Monitor PostgreSQL connection pool
4. Check response times in Network tab

### Performance Benchmarks

Target response times (with caching):
- Catalog browse: < 500ms
- Product detail: < 200ms
- Search: < 1s
- Vendor list: < 300ms

## Integration Testing

Run the full test suite:

```bash
dotnet test HalalChain.Platform.sln -c Release
```

Or run Marketplace-specific tests:

```bash
dotnet test HalalChain.Marketplace.Tests -c Release
```

## Cleanup

To reset the development environment:

```bash
# Stop containers
docker compose down

# Remove database volume (data loss!)
docker volume rm waklu-keewak_postgres_data

# Clean build artifacts
dotnet clean HalalChain.Platform.sln

# Rebuild everything
docker compose up --build
```

## Next Steps

After successful testing:
1. Fix any connection/mapping issues found
2. Implement advanced pages (analytics, product management)
3. Add real-time SignalR updates
4. Implement performance optimizations (caching, indexing)
5. Add unit tests for repositories and components
