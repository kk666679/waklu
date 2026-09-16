using System.IdentityModel.Tokens.Jwt;
using System.Text;
using HalalChain.Platform.Contracts.Auth;
using HalalChain.Application.Catalog.Handlers;
using HalalChain.Application.Common.Mappings;
using HalalChain.Application.Policies;
using HalalChain.Application.Vendors.Handlers;
using HalalChain.Platform.Api.AI;
using HalalChain.Platform.Api.Modules.Catalog;
using HalalChain.Platform.Api.Modules.Halal;
using HalalChain.Platform.Api.Modules.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using HalalChain.Platform.Api.Modules.Auth;
using HalalChain.Platform.Api.Modules.Blockchain;
using HalalChain.Platform.Api.Modules.Indexer;
using HalalChain.Platform.Api.Modules.Ipfs;
using HalalChain.Platform.Api.Modules.Vendors;
using HalalChain.Platform.Api.Persistence;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

// ── .env (local dev convenience) ───────────────────────────────────────────
// Real secrets must come from environment variables, not appsettings.json.
// The repo-root `.env` is a local development convenience that feeds env vars
// before the host is built. It is gitignored and never consulted in production
// (where docker-compose injects env vars directly). Existing env vars win so
// explicit deployment overrides still take precedence.
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// ── Data Protection ────────────────────────────────────────────────────────
// Persist keys to a stable, non-user-profile directory so they survive restarts
// and the "keys may not be persisted" warning is avoided.
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, ".aspnet", "DataProtection-Keys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("HalalChainPlatform")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// ── Controllers + API Versioning ──────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1");

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Problem Details ────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HalalChainDbContext>("database");

// ── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.PermitLimit = 50;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// ── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("HalalChainCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// ── JWT ───────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing Jwt configuration.");
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

if (!builder.Environment.IsDevelopment() &&
    string.Equals(jwtSettings.Key, "HalalChainDevJwtSecret2024ForTestingOnly!", StringComparison.Ordinal))
    throw new InvalidOperationException("Jwt:Key is set to the development example value. Configure a real key for non-development environments.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = AuthConstants.RoleClaimType
        };
    });

builder.Services.AddScoped<ICurrentUser, HalalChain.Platform.Api.Infrastructure.Security.HttpContextCurrentUser>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITokenService, HalalChain.Platform.Api.Modules.Auth.TokenService>();

// ── AI Inference client ───────────────────────────────────────────────────
builder.Services.AddHttpClient("AiInference", (sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiGatewayOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Semantic Search ───────────────────────────────────────────────────────
builder.Services.AddScoped<SemanticSearchService>();
builder.Services.AddAuthorization(options =>
{
    CatalogPolicies.Register(options);
    VendorPolicies.Register(options);
});

// ── MediatR + FluentValidation + AutoMapper (Application Layer) ─────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateProductHandler).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductRequestValidator).Assembly);
builder.Services.AddApplicationMapping();
builder.Services.AddScoped<HalalChain.Application.Common.Interfaces.IProductRepository, EfProductRepository>();
builder.Services.AddScoped<HalalChain.Application.Vendors.Interfaces.IVendorRepository, EfVendorRepository>();

// ── AI Gateway ────────────────────────────────────────────────────────────
builder.Services.Configure<AiGatewayOptions>(builder.Configuration.GetSection(AiGatewayOptions.SectionName));
builder.Services.AddHttpClient<IAiInferenceProvider, TransformersJsInferenceProvider>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiGatewayOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

// ── Tawheed client ────────────────────────────────────────────────────────
builder.Services.Configure<TawheedOptions>(builder.Configuration.GetSection(TawheedOptions.SectionName));
builder.Services.AddHttpClient<ITawheedClient, TawheedHttpClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TawheedOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ── Event Bus + Outbox ────────────────────────────────────────────────────
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IEventHandlerRegistry, EventHandlerRegistry>();
builder.Services.AddScoped<IEventBus, InProcessEventBus>();
builder.Services.AddHostedService<OutboxBackgroundService>();
builder.Services.AddHostedService<ProductTaxonomyBootstrapService>();

// ── Redis ─────────────────────────────────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache();

// ── EF Core (PostgreSQL when configured, SQLite fallback for local dev) ──
var pgConn = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(pgConn))
{
    builder.Services.AddDbContext<HalalChainDbContext>(o =>
    {
        o.UseNpgsql(pgConn);
        o.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
}
else
{
    var sqliteConn = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=halalchain.db";
    builder.Services.AddDbContext<HalalChainDbContext>(o =>
    {
        o.UseSqlite(sqliteConn);
        // Suppress the "model changes each time" warning that fires on
        // HasData seed when static values compare non-equal across two
        // builds. Our seed values are compile-time constants; the
        // warning is a false positive. See: aka.ms/efcore-docs-configure-warnings
        o.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });
    Console.WriteLine($"[startup] Using SQLite at: {sqliteConn}");
}

// ── Blockchain + IPFS + Indexer (optional, gracefully degraded) ──
// If Blockchain:RpcUrl is missing, the app runs without these modules;
// the verification endpoint will return 503 "chain unreachable".
if (!string.IsNullOrWhiteSpace(builder.Configuration["Blockchain:RpcUrl"]))
{
    builder.Services.AddSingleton<IWalletProvider, ConfigurationWalletProvider>();
    builder.Services.AddSingleton<ITransactionQueue, TransactionQueue>();
    builder.Services.AddSingleton<ISmartContractService, SmartContractService>();
    builder.Services.AddHostedService<OutboxDispatcherService>();
    builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();
    builder.Services.AddHostedService<EventIndexerHostedService>();
    Console.WriteLine("[startup] Blockchain module enabled.");
}
else
{
    Console.WriteLine("[startup] Blockchain module DISABLED (no RpcUrl). Verification endpoint will return 503.");
}

// IPFS storage (provider-pluggable; default kubo-local in dev, pinata in prod)
builder.Services.AddSingleton<ContentValidator>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["IPFS:KuboApiUrl"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["IPFS:PinataJwt"]))
{
    var provider = builder.Configuration["IPFS:Provider"] ?? "kubo-local";
    if (string.Equals(provider, "pinata", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHttpClient<IStorageService, PinataStorageService>();
    }
    else
    {
        builder.Services.AddHttpClient<IStorageService, LocalKuboStorageService>();
    }
    Console.WriteLine($"[startup] IPFS module enabled (provider: {provider}).");
}
else
{
    builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();
    Console.WriteLine("[startup] IPFS module running on local fallback (no provider configured).");
}

var app = builder.Build();

// ── Apply pending EF Core migrations on startup ───────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HalalChainDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
}

// ── Correlation ID middleware ─────────────────────────────────────────────
app.Use(async (context, next) =>
{
    StringValues cid = default;
    if (context.Request.Headers.TryGetValue("X-Correlation-ID", out cid) && !StringValues.IsNullOrEmpty(cid))
    {
        context.TraceIdentifier = cid.ToString();
    }
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
        return Task.CompletedTask;
    });
    await next();
});

// ── Exception handling ────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("HalalChainCors");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// ── .env loader ────────────────────────────────────────────────────────────
// Real secrets must come from environment variables, not appsettings.json.
// The repo-root `.env` is a local development convenience that feeds env vars
// before the host is built. It is gitignored and never consulted in production
// (where docker-compose injects env vars directly). Existing env vars win so
// explicit deployment overrides still take precedence.
static void LoadDotEnv()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".env")))
        directory = directory.Parent;

    if (directory is null) return;
    var path = Path.Combine(directory.FullName, ".env");

    foreach (var line in File.ReadLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

        var separator = trimmed.IndexOf('=');
        if (separator <= 0) continue;

        var key = trimmed[..separator].Trim();
        var value = trimmed[(separator + 1)..].Trim().Trim('"', '\'');
        if (Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}

app.Run();
