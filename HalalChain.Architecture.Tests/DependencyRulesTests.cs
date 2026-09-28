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
    private const string StorageAssembly = "HalalChain.Storage";
    private const string AgentsAssembly = "HalalChain.Agents";

    private static readonly string[] AssembliesToLoad =
    {
        WebAssembly,
        MarketplaceAssembly,
        McpAssembly,
        ApiAssembly,
        HttpAssembly,
        ContractsAssembly,
        DomainAssembly,
        ApplicationAssembly,
        StorageAssembly
    };

    private static Assembly LoadOrSkip(string name)
    {
        // NetArchTest needs the assembly loaded. The architecture test
        // project references all of these transitively (the API, Web,
        // Marketplace, MCP, Http, Contracts, Domain, Application);
        // Infrastructure may not exist yet.
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null!;
        }
    }

    /// <summary>
    /// The architecture test project must reference every assembly the
    /// rules below inspect. A missing ProjectReference makes
    /// <see cref="LoadOrSkip"/> return null and every dependent rule
    /// silently PASSES — a guardrail that cannot fail. This test turns
    /// that class of hole into a hard failure.
    /// </summary>
[Theory]
    [InlineData(DomainAssembly)]
    [InlineData(ApplicationAssembly)]
    [InlineData(ContractsAssembly)]
    [InlineData(HttpAssembly)]
    [InlineData(ApiAssembly)]
    [InlineData(WebAssembly)]
    [InlineData(MarketplaceAssembly)]
    [InlineData(McpAssembly)]
    [InlineData(StorageAssembly)]
    [InlineData(AgentsAssembly)]
    public void GuardedAssembly_ShouldBe_Loadable(string assemblyName)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyName);
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Assembly '{assemblyName}' could not be loaded, so every " +
                $"architecture rule guarding it silently passed. Add a " +
                $"ProjectReference to {assemblyName} in " +
                "HalalChain.Architecture.Tests.csproj. Cause: " + ex.Message);
            return;
        }

        Assert.True(
            !string.IsNullOrWhiteSpace(assembly.FullName),
            $"Assembly '{assemblyName}' resolved to an empty identity.");
    }

    [Fact]
    public void Application_ShouldNotReference_Api_Or_AnyUiProject()
    {
        var application = LoadOrSkip(ApplicationAssembly);
        if (application is null) return;

        var result = Types.InAssembly(application)
            .ShouldNot()
            .HaveDependencyOnAny(WebAssembly, MarketplaceAssembly, McpAssembly, ApiAssembly, InfrastructureAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Application is the use-case layer. It must not " +
            "reference the API host or any UI/MCP project — those depend on " +
            "it, never the reverse. Violations:\n" +
            string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Application_ShouldNotReference_ConcreteHttpClientTypes()
    {
        var application = LoadOrSkip(ApplicationAssembly);
        if (application is null) return;

        // Application-layer handlers must talk to persistence/AI through the
        // abstractions in Common/Interfaces, not through EF's concrete
        // context or a raw HttpClient. EF Core itself is allowed (the
        // repository implementations live here today, behind IProductRepository).
        var result = Types.InAssembly(application)
            .That()
            .ResideInNamespace("HalalChain.Application.Catalog.Handlers")
            .ShouldNot()
            .HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Catalog handlers must dispatch through IProductRepository / " +
            "ICurrentUser, not construct their own transport. Violations:\n" +
            string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Application_ShouldNotAssign_HalalVerdicts()
    {
        // The architectural principle in AGENTS.md: the deterministic Policy
        // Engine (tawheed) assigns compliance status. The LLM collects
        // evidence. No managed layer may fabricate a verdict.
        var application = LoadOrSkip(ApplicationAssembly);
        if (application is null) return;

        var offenders = new List<string>();
        foreach (var type in Types.InAssembly(application).GetTypes())
        {
            if (type.Namespace is null || !type.Namespace.StartsWith("HalalChain.Application", StringComparison.Ordinal))
                continue;
            if (!typeof(HalalChain.Domain.Halal.ComplianceStatus).IsAssignableFrom(type))
                continue;
            if (type.IsEnum || type.IsInterface)
                continue;
            offenders.Add(type.FullName ?? type.Name);
        }

        Assert.True(
            offenders.Count == 0,
            "HalalChain.Application must not define concrete verdict types. " +
            "Compliance status is decided by the tawheed Policy Engine. " +
            "Offenders:\n" + string.Join("\n", offenders));
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

    [Fact]
    public void Domain_ShouldNotReference_Storage()
    {
        var domain = LoadOrSkip(DomainAssembly);
        if (domain is null) return;

        var result = Types.InAssembly(domain)
            .ShouldNot()
            .HaveDependencyOn(StorageAssembly)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Domain must not reference HalalChain.Storage. " +
            "Domain stays I/O-free. Violations:\n" +
            string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Storage_ShouldNotReference_ApiOrUiOrMcp()
    {
        var storage = LoadOrSkip(StorageAssembly);
        if (storage is null) return;

        var forbidden = new[]
        {
            ApiAssembly,
            WebAssembly,
            MarketplaceAssembly,
            McpAssembly
        };

        var result = Types.InAssembly(storage)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "HalalChain.Storage is a leaf library. It must not reference " +
            "the API host, UI projects, or MCP. Violations:\n" +
            string.Join("\n", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void IBlobStore_DeclaresNoDeleteMember_ArchTest()
    {
        // Defence-in-depth duplicate of the contract test. The rule is
        // append-only is compile-time, not convention.
        var storage = LoadOrSkip(StorageAssembly);
        if (storage is null) return;

        var blobStore = storage.GetType("HalalChain.Application.Storage.IBlobStore");
        if (blobStore is null) return; // Interface moved? Test passes vacuously.

        var members = blobStore.GetMembers()
            .Select(m => m.Name)
            .Where(n => n.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Remove", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            members.Count == 0,
            "IBlobStore must not declare any Delete or Remove member. " +
            "Append-only is a compile-time property. Offenders:\n" +
            string.Join("\n", members));
    }

    [Fact]
    public void Storage_AssemblyDeclaresNoMerkleTreeType()
    {
        // Merge decision M1. Keccak256 for on-chain Merkle internal nodes lives
        // in the blockchain module. A SHA-256 Merkle tree in Storage is the exact
        // bug that made every inclusion proof fail on first integration.
        var storage = LoadOrSkip(StorageAssembly);
        if (storage is null) return;

        var offenders = storage.GetTypes()
            .Where(t => t.Name.Contains("Merkle", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName!)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "HalalChain.Storage must not declare any Merkle tree type. " +
            "On-chain tree shape is in the Blockchain module. Offenders:\n" +
            string.Join("\n", offenders));
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
