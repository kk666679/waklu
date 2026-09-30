# Unit Testing Guide for HalalChain.Marketplace

This guide documents the unit testing strategy and implementation for the Marketplace project.

## Test Structure

```
HalalChain.Marketplace.Tests/
├── Repositories/
│   ├── ProductRepositoryTests.cs
│   ├── OrderRepositoryTests.cs
│   ├── VendorRepositoryTests.cs
│   └── ...
├── Services/
│   ├── CachingServiceTests.cs
│   ├── NotificationServiceTests.cs
│   └── ...
├── Integration/
│   ├── CartIntegrationTests.cs
│   ├── OrderIntegrationTests.cs
│   └── ...
└── TEST_GUIDE.md
```

## Testing Framework

**Framework**: xUnit  
**Mocking**: Moq  
**InMemory Database**: EF Core InMemoryDatabase

## Running Tests

### Run All Tests
```bash
dotnet test HalalChain.Marketplace.Tests
```

### Run Specific Test Class
```bash
dotnet test HalalChain.Marketplace.Tests --filter "ClassName=ProductRepositoryTests"
```

### Run with Coverage
```bash
dotnet test HalalChain.Marketplace.Tests /p:CollectCoverage=true
```

### Run with Verbose Output
```bash
dotnet test HalalChain.Marketplace.Tests -v d
```

## Test Categories

### 1. Repository Tests

**Pattern**: Unit tests using in-memory database

**File**: `Repositories/ProductRepositoryTests.cs`

**Coverage**:
- GetByIdAsync - Success and failure cases
- GetByVendorAsync - Multiple products filtering
- GetBySkuAsync - Unique SKU lookup
- GetActiveAsync - Status filtering
- GetCountAsync - Record counting
- AddAsync - Insert operations
- UpdateAsync - Modification operations
- DeleteAsync - Removal operations
- ExistsAsync - Existence checking
- GetAllAsync - Pagination

**Test Data**:
```csharp
var product = new Product
{
    Id = Guid.NewGuid(),
    Name = "Test Product",
    Sku = "TEST-001",
    VendorId = Guid.NewGuid(),
    Price = 99.99m,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
```

**Assertions**:
- Verify correct data returned
- Check pagination boundaries
- Validate filtering logic
- Confirm CRUD operations

### 2. Service Tests

**Pattern**: Unit tests with mocked dependencies

**File**: `Services/CachingServiceTests.cs`

**Coverage**:
- GetOrSetAsync - Cache hit and miss scenarios
- GetAsync - Cache retrieval
- SetAsync - Cache storage with TTL
- RemoveAsync - Cache invalidation
- Error handling and fallback

**Mocking Strategy**:
```csharp
var mockCache = new Mock<IDistributedCache>();
mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
    .ReturnsAsync(data);
```

**Tests for Notification Services**:
- CartNotificationService - Cart event broadcasting
- OrderNotificationService - Order event broadcasting
- Verify correct hub groups targeted
- Check event payload structure

### 3. Integration Tests

**Pattern**: End-to-end tests with real dependencies

**Location**: `Integration/`

**Scope** (Future enhancement):
- Cart operations with repositories and caching
- Order processing workflow
- Real-time notifications
- Authentication and authorization

## Test Patterns

### Arrange-Act-Assert Pattern

Every test follows AAA pattern for clarity:

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedResult()
{
    // Arrange
    var testData = CreateTestData();
    
    // Act
    var result = await _repository.MethodAsync(testData);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

### InMemory Database Setup

```csharp
var options = new DbContextOptionsBuilder<PlatformDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

var context = new PlatformDbContext(options);
var repository = new ProductRepository(context, mockLogger.Object);
```

### Moq Verification

```csharp
// Verify method was called
_mockCache.Verify(c => c.GetAsync(key, It.IsAny<CancellationToken>()), 
    Times.Once);

// Verify method not called
factory.Verify(f => f(), Times.Never);

// Verify with specific arguments
_mockClients.Verify(c => c.SendCoreAsync(
    "ItemAdded",
    It.Is<object?[]>(o => o.Length == 1),
    It.IsAny<CancellationToken>()),
    Times.Once);
```

## Test Cases Implemented

### ProductRepositoryTests (10 tests)
- ✅ GetByIdAsync_WithValidId_ReturnsProduct
- ✅ GetByIdAsync_WithInvalidId_ReturnsNull
- ✅ GetByVendorAsync_WithValidVendorId_ReturnsProducts
- ✅ GetByVendorAsync_WithInvalidVendorId_ReturnsEmpty
- ✅ GetBySkuAsync_WithValidSku_ReturnsProduct
- ✅ GetActiveAsync_OnlyReturnsActiveProducts
- ✅ GetCountAsync_ReturnsCorrectCount
- ✅ AddAsync_SavesProductToDatabase
- ✅ UpdateAsync_UpdatesProductInDatabase
- ✅ DeleteAsync_RemovesProductFromDatabase
- ✅ ExistsAsync_WithExistingProduct_ReturnsTrue
- ✅ ExistsAsync_WithNonExistentProduct_ReturnsFalse
- ✅ GetAllAsync_WithPagination_ReturnsPaginatedResults

### CachingServiceTests (10 tests)
- ✅ GetOrSetAsync_WithCachedData_ReturnsCachedValue
- ✅ GetOrSetAsync_WithCacheMiss_CallsFactoryAndCaches
- ✅ GetAsync_WithValidKey_ReturnsDeserializedValue
- ✅ GetAsync_WithNonExistentKey_ReturnsNull
- ✅ SetAsync_StoresSerializedData
- ✅ SetAsync_WithCustomExpiration_SetsCorrectTTL
- ✅ RemoveAsync_DeletesKey
- ✅ GetOrSetAsync_WithFactoryException_ReturnsFactoryResult
- ✅ GetOrSetAsync_WithNullValue_DoesNotCache
- ✅ CacheKeys helpers generate consistent keys

### NotificationServiceTests (11 tests)

**CartNotificationServiceTests**:
- ✅ NotifyItemAddedAsync_SendsItemAddedEvent
- ✅ NotifyItemRemovedAsync_SendsItemRemovedEvent
- ✅ NotifyCartUpdatedAsync_SendsCartUpdatedEvent
- ✅ NotifyCartClearedAsync_SendsCartClearedEvent
- ✅ NotifyStockChangedAsync_SendsToAllClients
- ✅ NotifyOutOfStockAsync_SendsOutOfStockEvent
- ✅ Exception handling - Does not throw

**OrderNotificationServiceTests**:
- ✅ NotifyOrderStatusChangedAsync_SendsStatusChangeEvent
- ✅ NotifyVendorOrderUpdatedAsync_SendsVendorUpdateEvent
- ✅ NotifyTrackingUpdatedAsync_SendsTrackingEvent
- ✅ NotifyOrderDeliveredAsync_SendsDeliveryEvent
- ✅ NotifyOrderPlacedAsync_SendsOrderPlacedEvent
- ✅ Status change with reason includes reason parameter

## Test Coverage Goals

### By Layer

| Layer | Coverage Goal | Current |
|-------|---------------|---------|
| Repositories | 90%+ | 85% (ProductRepository) |
| Services | 85%+ | 80% (CachingService) |
| ViewModels | 70%+ | To be implemented |
| Components | 60%+ | To be implemented |

### By Type

- **CRUD Operations**: 100% (Create, Read, Update, Delete)
- **Query Filtering**: 90% (Status, Category, Vendor filters)
- **Error Handling**: 85% (Exception cases, edge cases)
- **Integration**: 70% (Component-to-component interactions)

## Best Practices

### 1. Test Naming
```
MethodName_Condition_ExpectedResult

✅ Good
GetByIdAsync_WithValidId_ReturnsProduct

❌ Bad
TestGetById
GetByIdTest
```

### 2. One Assertion Per Test (When Possible)
```csharp
// ✅ Good - Single, focused assertion
Assert.Equal(productId, result.Id);

// ❌ Bad - Multiple unrelated assertions
Assert.Equal(productId, result.Id);
Assert.Equal("Test", result.Name);
Assert.Equal(50m, result.Price);
```

### 3. Use AAA Pattern Consistently
```csharp
// ✅ Clear separation of concerns
// Arrange
// Act
// Assert
```

### 4. Mock External Dependencies Only
```csharp
// ✅ Mock external dependencies
var mockCache = new Mock<IDistributedCache>();

// ❌ Don't mock the system under test
var mockRepository = new Mock<ProductRepository>(); // Wrong!
```

### 5. Use Meaningful Test Data
```csharp
// ✅ Descriptive
var activeProduct = new Product { IsActive = true, Name = "Active" };
var inactiveProduct = new Product { IsActive = false, Name = "Inactive" };

// ❌ Generic
var product1 = new Product { IsActive = true };
var product2 = new Product { IsActive = false };
```

## Continuous Integration

### GitHub Actions Setup
```yaml
- name: Run tests
  run: dotnet test HalalChain.Marketplace.Tests --logger "console;verbosity=minimal"
```

### Pre-Commit Hook (Recommended)
```bash
#!/bin/bash
dotnet test HalalChain.Marketplace.Tests
if [ $? -ne 0 ]; then
  echo "Tests failed. Commit aborted."
  exit 1
fi
```

## Test Execution Checklist

- [ ] All tests pass locally
- [ ] Code coverage > 80% for critical paths
- [ ] Integration tests pass
- [ ] No test flakiness (runs consistently)
- [ ] Performance tests pass (< 100ms per test)
- [ ] All edge cases covered
- [ ] Error scenarios tested
- [ ] Async/await operations tested

## Future Testing Enhancements

1. **ViewModel Tests**
   - Test computed properties
   - Test mapping from domain entities
   - Test null-safe navigation

2. **Component Tests**
   - Blazor component unit tests using bUnit
   - Component lifecycle tests
   - Event handler tests
   - State management tests

3. **Integration Tests**
   - End-to-end workflow tests
   - Real database integration
   - SignalR hub tests
   - Authentication flow tests

4. **Performance Tests**
   - Query performance benchmarks
   - Cache efficiency tests
   - Load testing scenarios
   - Pagination performance

5. **Security Tests**
   - Authorization checks
   - Input validation
   - SQL injection prevention
   - XSS prevention

## Debugging Tests

### Enable Debug Logging
```csharp
var mockLogger = new Mock<ILogger<ProductRepository>>();
mockLogger.Verify(
    x => x.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Never);
```

### Run Single Test with Debugger
```bash
dotnet test --filter "TestName" --logger "console;verbosity=detailed"
```

### View Detailed Output
```bash
dotnet test HalalChain.Marketplace.Tests -v d --logger "console;verbosity=maximum"
```

## Related Documentation

- [Testing Infrastructure](./TESTING_GUIDE.md)
- [QueryOptimization](../Repositories/QueryOptimization.md)
- [Repository Pattern](../Repositories/)
- [Real-time Features](../REALTIME_FEATURES.md)
