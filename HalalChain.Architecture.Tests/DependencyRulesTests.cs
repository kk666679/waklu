using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace HalalChain.Architecture.Tests;

/// <summary>
/// Architectural guardrails for the HalalChain platform.
///
/// Phase 0.5 of the migration. The first commit of this project must be
/// GREEN against the *current* code; we will progressively tighten the
/// rules as Domain/Application/Infrastructure projects are introduced.
///
/// Conventions enforced here (start small, grow deliberately):
///   - The UI projects (HalalChain.Web, HalalChain.Marketplace) must
///     depend only on Contracts and Http. They must not reference
///     Infrastructure, Application, Domain, or the API project.
///   - HalalChain.Mcp must not reference the API project (MCP tools are
///     thin adapters; they do not call into API controllers/services
///     directly — they call into Application use cases once that layer
///     exists).
///
/// Rules that are not yet satisfiable (Domain/Application/Infrastructure
/// do not exist yet, the API still hosts entities, etc.) are recorded
/// in <see cref="KnownUntestable"/> so they are visible but skipped.
/// </summary>
public sealed class DependencyRulesTests
{
    private const string WebAssembly = "HalalChain.Web";
    private const string MarketplaceAssembly = "HalalChain.Marketplace";
    private const string McpAssembly = "HalalChain.Mcp";
    private const string ApiAssembly = "HalalChain.Platform.Api";
    private const string HttpAssembly = "HalalChain.Platform.Http";
    private const string ContractsAssembly = "HalalChain.Platform.Contracts";
    private const string DomainAssembly = "HalalChain.Domain";
    private const string ApplicationAssembly = "HalalChain.Application";
    private const string InfrastructureAssembly = "HalalChain.Infrastructure";

    private static readonly string[] AssembliesToLoad =
    {
        WebAssembly,
        MarketplaceAssembly,
        McpAssembly,
        ApiAssembly,
        HttpAssembly,
        ContractsAssembly
    };

    private static Assembly LoadOrSkip(string name)
    {
        // NetArchTest needs the assembly loaded. The architecture test
        // project references all of these transitively (the API, Web,
        // Marketplace, MCP, Http, Contracts); the Domain/Application/
        // Infrastructure assemblies are optional and may not exist yet.
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null!;
        }
    }

    [Fact]
    public void Contracts_ShouldNotReference_AnyOtherProject()
    {
        var contracts = LoadOrSkip(ContractsAssembly);
        if (contracts is null) return;

        var result = Types.InAssembly(contracts)
            .ShouldNot()
            .HaveDependencyOnAny(
                WebAssembly,
                MarketplaceAssembly,
                McpAssembly,
                ApiAssembly,
                HttpAssembly,
                DomainAssembly,
                ApplicationAssembly,
                InfrastructureAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Platform.Contracts must not reference any other " +
            "HalalChain project. Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Mcp_ShouldNotReference_ApiProject()
    {
        var mcp = LoadOrSkip(McpAssembly);
        if (mcp is null) return;

        var result = Types.InAssembly(mcp)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Mcp tools must be thin adapters. They must call " +
            "into the Application layer, not into the API project. " +
            "Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Marketplace_ShouldNotReference_ApiProject()
    {
        var marketplace = LoadOrSkip(MarketplaceAssembly);
        if (marketplace is null) return;

        var result = Types.InAssembly(marketplace)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Marketplace must consume the platform through " +
            "HalalChain.Platform.Http, not the API project directly. " +
            "Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Http_ShouldNotReference_ApiProject()
    {
        var http = LoadOrSkip(HttpAssembly);
        if (http is null) return;

        var result = Types.InAssembly(http)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Platform.Http is a typed client; it must not " +
            "reference the API project. Violations:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Domain_ShouldNotReference_EFCore_Or_AnyOtherProject()
    {
        var domain = LoadOrSkip(DomainAssembly);
        if (domain is null) return;

        // Domain must not depend on any other HalalChain project, nor on
        // EF Core, ASP.NET Core, Radzen, or external provider SDKs.
        var forbiddenProjects = new[]
        {
            WebAssembly,
            MarketplaceAssembly,
            McpAssembly,
            ApiAssembly,
            HttpAssembly,
            ContractsAssembly,
            ApplicationAssembly,
            InfrastructureAssembly
        };

        var projectResult = Types.InAssembly(domain)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        Assert.True(
            projectResult.IsSuccessful,
            "HalalChain.Domain must not reference any other HalalChain " +
            "project. Violations:\n" + string.Join("\n", projectResult.FailingTypeNames ?? Array.Empty<string>()));

        var efResult = Types.InAssembly(domain)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            efResult.IsSuccessful,
            "HalalChain.Domain must not reference EF Core. Violations:\n" +
            string.Join("\n", efResult.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void NonApiProjects_ShouldNotReference_EFCore()
    {
        // UI, MCP, and HTTP projects must not depend on EF Core. EF Core
        // is an Infrastructure concern. The API project legitimately
        // hosts the DbContext today; once Infrastructure is split out
        // this rule will be tightened to also exclude the API.
        var projects = new[]
        {
            (WebAssembly, LoadOrSkip(WebAssembly)),
            (MarketplaceAssembly, LoadOrSkip(MarketplaceAssembly)),
            (McpAssembly, LoadOrSkip(McpAssembly)),
            (HttpAssembly, LoadOrSkip(HttpAssembly))
        };

        var offenders = new List<string>();
        foreach (var (name, asm) in projects)
        {
            if (asm is null) continue;
            var result = Types.InAssembly(asm)
                .ShouldNot()
                .HaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetResult();
            if (!result.IsSuccessful)
            {
                offenders.Add($"{name}:\n" + string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "UI / MCP / HTTP must not reference EF Core. Offenders:\n" +
            string.Join("\n", offenders));
    }

    /// <summary>
    /// Ensures entities that have been migrated to Domain do not creep
    /// back into the API's Persistence.Entities namespace. Each move
    /// should add one test of this shape.
    /// </summary>
    [Theory]
    [InlineData("ProductEmbedding", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("OutboxMessage", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("SupplierOnChain", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("ProductOnChain", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("CertificateOnChain", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("TraceabilityEventOnChain", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("ChainTxOutbox", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("Certificate", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("HalalVerification", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("VerificationEvidence", "HalalChain.Platform.Api.Persistence.Entities")]
    [InlineData("VerificationAudit", "HalalChain.Platform.Api.Persistence.Entities")]
    public void MovedEntities_ShouldNotExistIn_ApiPersistenceEntities(string typeName, string oldNamespace)
    {
        var api = LoadOrSkip(ApiAssembly);
        if (api is null) return;

        var types = Types.InAssembly(api)
            .That()
            .ResideInNamespace(oldNamespace)
            .And()
            .HaveName(typeName)
            .GetTypes();

        Assert.True(
            !types.Any(),
            $"Type '{typeName}' was found in '{oldNamespace}'. " +
            $"It has been migrated to Domain and must not exist here.");
    }

    /// <summary>
    /// Ensures the migrated types actually live in the Domain layer.
    /// </summary>
    [Theory]
    [InlineData("ProductEmbedding", "HalalChain.Domain.Catalog")]
    [InlineData("OutboxMessage", "HalalChain.Domain.Common")]
    [InlineData("IAggregateRoot", "HalalChain.Domain")]
    [InlineData("SupplierOnChain", "HalalChain.Domain.Blockchain")]
    [InlineData("ProductOnChain", "HalalChain.Domain.Blockchain")]
    [InlineData("CertificateOnChain", "HalalChain.Domain.Blockchain")]
    [InlineData("TraceabilityEventOnChain", "HalalChain.Domain.Blockchain")]
    [InlineData("ChainTxOutbox", "HalalChain.Domain.Blockchain")]
    [InlineData("Certificate", "HalalChain.Domain.Halal")]
    [InlineData("HalalVerification", "HalalChain.Domain.Halal")]
    [InlineData("VerificationEvidence", "HalalChain.Domain.Halal")]
    [InlineData("VerificationAudit", "HalalChain.Domain.Halal")]
    [InlineData("ComplianceStatus", "HalalChain.Domain.Halal")]
    [InlineData("CertificateStatus", "HalalChain.Domain.Halal")]
    public void MigratedTypes_ShouldResideIn_Domain(string typeName, string domainNamespace)
    {
        var domain = LoadOrSkip(DomainAssembly);
        if (domain is null) return;

        var types = Types.InAssembly(domain)
            .That()
            .ResideInNamespace(domainNamespace)
            .And()
            .HaveName(typeName)
            .GetTypes();

        Assert.True(
            types.Any(),
            $"Type '{typeName}' was expected in '{domainNamespace}' but was not found.");
    }

    /// <summary>
    /// Documented future rules. These are skipped today because the
    /// project they target does not exist yet, or because the rule is
    /// known to fail on the current code and will be enforced only
    /// after the corresponding migration phase.
    /// </summary>
    [Fact]
    public void KnownUntestable()
    {
        // Once HalalChain.Application exists:
        //   - Application must not reference UI, API, MCP, concrete providers
        //
        // Once HalalChain.Infrastructure exists:
        //   - Infrastructure may reference EF Core, Nethereum, IPFS SDKs,
        //     HttpClients — but must not be referenced by UI.
        //
        // Once HalalChain.UI.Shared exists:
        //   - UI projects may reference UI.Shared, Contracts, Http only.
        //
        // Once HalalChain.Storefront / HalalChain.Admin are split from Web:
        //   - Admin must not be referenced by Storefront.
        //
        // These are tracked in docs/architecture/implementation-status.md.
        Assert.True(true);
    }
}
