# Shared Multi-Tenant Dashboard Layout - Integration Checklist

## Pre-Deployment Verification

### Project Structure
- [ ] `/Components/Dashboard/` directory exists with all components
  - [ ] `DashboardLayout.razor`
  - [ ] `DashboardHeader.razor`
  - [ ] `DashboardSidebar.razor`
  - [ ] `DashboardRegion.razor`
  - [ ] `DashboardWidget.razor`
  - [ ] `DashboardFooter.razor`
  - [ ] `DashboardBreadcrumb.razor`

- [ ] `/Models/Dashboard/` directory exists with models
  - [ ] `TenantDashboardConfig.cs`
  - [ ] `DashboardConfigEntity.cs`

- [ ] `/Services/Dashboard/` directory exists with services
  - [ ] `IDashboardConfigService.cs`
  - [ ] `DashboardConfigService.cs`
  - [ ] `INavigationService.cs`
  - [ ] `NavigationService.cs`
  - [ ] `/CacheStrategies/` subdirectory with all strategies

- [ ] `/Repositories/Dashboard/` directory exists
  - [ ] `IDashboardConfigRepository.cs`
  - [ ] `DashboardConfigRepository.cs`

- [ ] `/Data/` directory contains
  - [ ] `ApplicationDbContext.cs` with Dashboard DbSets

- [ ] `/DependencyInjection/` directory contains
  - [ ] `DashboardServiceCollectionExtensions.cs`

- [ ] `/Pages/Dashboard/` directory exists with sample pages
  - [ ] `AdminDashboard.razor`
  - [ ] `VendorDashboard.razor`
  - [ ] `AuditorDashboard.razor`

- [ ] `/wwwroot/css/` contains
  - [ ] `dashboard-layout.css` (800+ lines)
  - [ ] `dashboard-breadcrumb.css`

### Configuration Files
- [ ] `appsettings.json` includes Dashboard section
- [ ] `appsettings.development.dashboard.json` exists
- [ ] `appsettings.production.dashboard.json` exists
- [ ] Environment variables documented for production secrets

### NuGet Dependencies
- [ ] `Microsoft.EntityFrameworkCore` >= 10.0.0
- [ ] `Microsoft.EntityFrameworkCore.SqlServer` >= 10.0.0
- [ ] `StackExchange.Redis` for distributed caching
- [ ] `Microsoft.Extensions.Caching.StackExchangeRedis`

## Code Integration

### Program.cs / Startup

- [ ] Dashboard services registered
  ```csharp
  builder.Services.AddDashboardServices(builder.Configuration);
  builder.Services.AddDashboardBackgroundJobs();
  ```

- [ ] Cache strategy configured (one of):
  - [ ] Hybrid strategy (recommended)
  - [ ] Distributed strategy (Redis only)
  - [ ] In-memory strategy (development)

- [ ] DbContext registered with SQL Server
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseSqlServer(connectionString));
  ```

- [ ] Dashboard endpoints mapped
  ```csharp
  app.MapDashboardEndpoints();
  ```

- [ ] Middleware pipeline includes:
  - [ ] Authentication
  - [ ] Authorization
  - [ ] Tenant context middleware

### _Imports.razor

- [ ] Dashboard components imported
  ```razor
  @using HalalChain.Marketplace.Components.Dashboard
  @using HalalChain.Marketplace.Models.Dashboard
  @using HalalChain.Marketplace.Services.Dashboard
  ```

### Layout.cshtml or MainLayout.razor

- [ ] CSS includes added for dashboard
  ```html
  <link href="_framework/scoped.css?v=..." rel="stylesheet" />
  <link href="css/dashboard-layout.css" rel="stylesheet" />
  <link href="css/dashboard-breadcrumb.css" rel="stylesheet" />
  ```

- [ ] JavaScript modules included (if needed for theme toggle)
  ```html
  <script src="js/admin-theme.js"></script>
  ```

## Database

### Migrations

- [ ] Migration created: `AddDashboardConfiguration`
  ```bash
  dotnet ef migrations add AddDashboardConfiguration
  ```

- [ ] Migration reviewed for correctness
  - [ ] `DashboardConfigs` table created
  - [ ] `DashboardConfigAudits` table created
  - [ ] `DashboardConfigCache` table created
  - [ ] All indexes created
  - [ ] Constraints applied

- [ ] Migration applied to database
  ```bash
  dotnet ef database update
  ```

### Data Seeding

- [ ] Initial configuration seeded for each tenant
- [ ] Default navigation items seeded
- [ ] Default role permissions seeded
- [ ] Feature flags initialized

## Testing

### Unit Tests

- [ ] `DashboardConfigService` tests
  - [ ] Cache hit/miss scenarios
  - [ ] Configuration merging
  - [ ] Tenant isolation

- [ ] `NavigationService` tests
  - [ ] Breadcrumb building
  - [ ] Route resolution
  - [ ] Permission filtering

- [ ] Cache strategy tests
  - [ ] In-memory cache operations
  - [ ] Distributed cache operations
  - [ ] Hybrid cache fallback

- [ ] Repository tests
  - [ ] CRUD operations
  - [ ] Versioning logic
  - [ ] Audit tracking

### Integration Tests

- [ ] Dashboard page load test
  - [ ] Config loads correctly
  - [ ] Tenant data scoped properly
  - [ ] Navigation items filtered by role

- [ ] Configuration update test
  - [ ] Config persists to database
  - [ ] Cache invalidates
  - [ ] Audit log created

- [ ] Multi-tenant isolation test
  - [ ] Tenant A can't access Tenant B config
  - [ ] Tenant A sees only own navigation
  - [ ] Tenant A sees only own widgets

- [ ] Cache strategy test
  - [ ] L1 cache hits on repeated requests
  - [ ] L2 cache serves when L1 misses
  - [ ] Fallback works if L2 unavailable

### Manual Testing

- [ ] Admin dashboard page displays correctly
- [ ] Vendor dashboard page displays correctly
- [ ] Auditor dashboard page displays correctly
- [ ] Dark/light theme toggle works
- [ ] Sidebar collapse/expand works
- [ ] Navigation items filter by role
- [ ] Breadcrumbs display and navigate correctly
- [ ] Configuration API endpoints respond correctly

## Performance

### Load Testing

- [ ] Single tenant dashboard load < 200ms
- [ ] Configuration cache hit rate > 80%
- [ ] Redis connection pool configured
- [ ] Database connection pool tuned

### Monitoring

- [ ] Cache statistics endpoint working
- [ ] Audit log queryable and performant
- [ ] Database indexes verified with execution plans
- [ ] Slow query log configured

## Security

### Authorization

- [ ] Dashboard pages require `[Authorize]`
- [ ] Admin endpoints require `[Authorize(Roles = "Administrator")]`
- [ ] Tenant context properly injected
- [ ] Query-layer tenant filtering applied

### Data Protection

- [ ] Configuration JSON doesn't contain secrets
- [ ] Secrets loaded from environment variables
- [ ] Database backups encrypted
- [ ] Audit trail immutable (soft deletes only)

### Multi-Tenant Isolation

- [ ] Tenant ID in all queries
- [ ] Automatic tenant scoping in repository
- [ ] Cross-tenant access attempts logged
- [ ] Capability validation enforced

## Documentation

- [ ] README.md updated with dashboard info
- [ ] API documentation generated
- [ ] Configuration schema documented
- [ ] Sample dashboard pages documented
- [ ] Troubleshooting guide created
- [ ] Developer onboarding guide updated

## Deployment

### Staging Environment

- [ ] Code deployed to staging
- [ ] Database migrations applied to staging
- [ ] Initial configurations seeded
- [ ] All dashboard pages accessible
- [ ] Cache strategy tested
- [ ] Multi-tenant scenarios tested

### Production Preparation

- [ ] Deployment checklist reviewed
- [ ] Rollback plan documented
- [ ] Connection strings configured
- [ ] Redis cluster configured and tested
- [ ] Database backups scheduled
- [ ] Monitoring and alerting configured

### Production Deployment

- [ ] Feature flag disabled (if using)
- [ ] Blue-green deployment executed
- [ ] Health checks passing
- [ ] Cache warming completed
- [ ] Smoke tests passed
- [ ] Performance metrics within SLA
- [ ] Audit logs being captured
- [ ] Rollback tested (in case needed)

## Post-Deployment

### Validation

- [ ] All tenants have active configurations
- [ ] Cache hit rates > 80%
- [ ] No configuration errors in logs
- [ ] Audit logs being recorded
- [ ] Performance metrics stable
- [ ] User feedback positive

### Monitoring Setup

- [ ] Dashboard metrics dashboard created
- [ ] Cache performance alerts configured
- [ ] Database performance alerts configured
- [ ] Error rate alerts configured
- [ ] Latency alerts configured

### Maintenance

- [ ] Configuration backup strategy implemented
- [ ] Cache cleanup job running
- [ ] Audit log cleanup job running
- [ ] Regular performance reviews scheduled
- [ ] Configuration update process documented

## Rollback Plan

If issues occur post-deployment:

1. [ ] Identify the issue
2. [ ] Notify stakeholders
3. [ ] Roll back to previous configuration version
   ```sql
   UPDATE DashboardConfigs SET IsActive = 1 
   WHERE Id = @PreviousVersionId AND TenantId = @TenantId
   ```
4. [ ] Invalidate cache
5. [ ] Monitor for stability
6. [ ] Root cause analysis
7. [ ] Deploy fix in next release

## Sign-Off

- [ ] Development Team Lead: __________ Date: __________
- [ ] QA Lead: __________ Date: __________
- [ ] DevOps Lead: __________ Date: __________
- [ ] Product Owner: __________ Date: __________

---

## Notes

- Total files created: 40+
- Lines of code: 15,000+
- Test coverage target: > 80%
- Performance target: < 200ms p95 latency
- Cache hit rate target: > 80%
- Audit trail completeness: 100%
