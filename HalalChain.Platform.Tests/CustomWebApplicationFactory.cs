using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Vendors;
using HalalChain.Platform.Api.Persistence;

namespace HalalChain.Platform.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Snapshot of environment variables we mutate so we can restore them
    // deterministically after each test, keeping parallel test execution
    // safe even when multiple factories are created in the same process.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string?> _envSnapshot = new();
    private static readonly string[] _managedEnvKeys =
    {
        "Jwt__Key",
        "Jwt__Issuer",
        "Jwt__Audience",
        "ConnectionStrings__Postgres",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var k in _managedEnvKeys)
            _envSnapshot[k] = Environment.GetEnvironmentVariable(k);

        Environment.SetEnvironmentVariable("Jwt__Key", "test-secret-key-that-is-at-least-32-chars-long!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", "Host=localhost;Database=test");

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var toRemove = new List<ServiceDescriptor>();
            foreach (var d in services)
            {
                if (d.ServiceType == typeof(HalalChainDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<HalalChainDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                     d.ServiceType.GenericTypeArguments.Contains(typeof(HalalChainDbContext))) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)) ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                    (d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true &&
                     d.ImplementationType?.FullName?.Contains("InMemory") != true &&
                     d.ImplementationType?.FullName?.Contains("Extensions") == true))
                    toRemove.Add(d);
            }
            foreach (var d in toRemove.Distinct()) services.Remove(d);

            var authDescriptors = services.Where(d =>
                d.ServiceType == typeof(IConfigureOptions<AuthenticationOptions>) ||
                d.ServiceType == typeof(IConfigureNamedOptions<AuthenticationOptions>)
            ).ToList();
            foreach (var d in authDescriptors) services.Remove(d);

            services.AddDbContext<HalalChainDbContext>(options =>
            {
                options.UseInMemoryDatabase("HalalChainTestDb");
            });

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o =>
                {
                    o.ClaimsIssuer = "Test";
                });
            services.AddOptions<AuthenticationOptions>()
                .Configure(o =>
                {
                    o.DefaultAuthenticateScheme = "Test";
                    o.DefaultChallengeScheme = "Test";
                    o.DefaultForbidScheme = "Test";
                });
            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddHostedService<TestDbSeeder>();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Restore the prior values we snapshotted in ConfigureWebHost.
            // If a key was not present before, clear it so we don't leak
            // a test-only JWT key into other tests in the same process.
            foreach (var kvp in _envSnapshot)
            {
                if (kvp.Value == null)
                    Environment.SetEnvironmentVariable(kvp.Key, null);
                else
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
        base.Dispose(disposing);
    }
}

public sealed class TestDbSeeder : IHostedService
{
    private readonly HalalChainDbContext _db;
    public TestDbSeeder(HalalChainDbContext db) => _db = db;

    public async Task StartAsync(CancellationToken ct)
    {
        await _db.Database.EnsureCreatedAsync(ct);

        if (!await _db.Products.AnyAsync(ct))
        {
            var cat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Food & Beverages", Slug = "food-beverages" };
            var snack = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Snacks", Slug = "snacks", ParentId = cat.Id };
            _db.Categories.AddRange(cat, snack);

            var v1 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000001"), Name = "Selera Masak", Slug = "selera-masak", Status = "Active", Country = "MY" };
            var v2 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000002"), Name = "Al-Barakah Foods", Slug = "al-barakah", Status = "Active", Country = "MY" };
            _db.Vendors.AddRange(v1, v2);

            var now = DateTimeOffset.UtcNow;
            for (int i = 1; i <= 53; i++)
            {
                _db.Products.Add(new Product
                {
                    Id = Guid.Parse($"B{i:D3}0000-0000-0000-0000-000000000000"),
                    Title = $"Product {i}",
                    Slug = $"product-{i}",
                    CategoryId = i % 2 == 0 ? cat.Id : snack.Id,
                    VendorId = i % 2 == 0 ? v1.Id : v2.Id,
                    Price = 10m + i,
                    Currency = "MYR",
                    Inventory = 100,
                    Origin = "Malaysia",
                    CreatedAt = now.AddDays(-i)
                });
            }
            await _db.SaveChangesAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string RolesHeader = "X-Test-Roles";
    private const string NameHeader = "X-Test-Name";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var name = Request.Headers.TryGetValue(NameHeader, out var nameHeader) && !string.IsNullOrWhiteSpace(nameHeader.ToString())
            ? nameHeader.ToString()
            : "test-user";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, name),
            new(JwtRegisteredClaimNames.Sub, name),
        };

        if (Request.Headers.TryGetValue(RolesHeader, out var rolesHeader))
        {
            var roles = rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            claims.AddRange(roles.Select(r => new Claim("role", r)));
        }

        var identity = new ClaimsIdentity(claims, "Test", ClaimTypes.Name, "role");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestAuthHelper
{
    public static void SetTestRoles(this HttpClient client, params string[] roles)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Roles");
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
    }

    /// <summary>Simulates a vendor (or any subject) by setting the Name claim to a
    /// stable Guid and the requested roles. The Platform API derives VendorId from
    /// the Name claim, so this is how vendor-isolation tests get a real vendor identity.</summary>
    public static void SetTestUser(this HttpClient client, Guid subjectId, params string[] roles)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Name");
        client.DefaultRequestHeaders.Add("X-Test-Name", subjectId.ToString());
        client.SetTestRoles(roles);
    }
}
