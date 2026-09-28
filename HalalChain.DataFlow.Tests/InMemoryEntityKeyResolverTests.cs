using HalalChain.DataFlow.Transform;
using Xunit;

namespace HalalChain.DataFlow.Tests;

public sealed class InMemoryEntityKeyResolverTests
{
    private readonly InMemoryEntityKeyResolver _resolver = new();

    [Fact]
    public void Resolve_BeforeRegistration_ReturnsNull() =>
        Assert.Null(_resolver.Resolve("erp", "SUPPLIER", "SUP-1"));

    [Fact]
    public void RegisteredKey_Resolves()
    {
        _resolver.Register("erp", "SUPPLIER", "SUP-1", "canon-1");

        Assert.Equal("canon-1", _resolver.Resolve("erp", "SUPPLIER", "SUP-1"));
    }

    [Theory]
    [InlineData("SUP-1")]
    [InlineData("sup-1")]
    [InlineData("  SUP-1  ")]
    public void KeyLookup_IsCaseAndWhitespaceInsensitive(string variant)
    {
        _resolver.Register("erp", "SUPPLIER", "SUP-1", "canon-1");

        Assert.Equal("canon-1", _resolver.Resolve("erp", "SUPPLIER", variant));
    }

    [Fact]
    public void DifferentEntityTypes_DoNotCollide()
    {
        _resolver.Register("erp", "SUPPLIER", "SHARED-KEY", "supplier-1");
        _resolver.Register("erp", "FACILITY", "SHARED-KEY", "facility-1");

        Assert.Equal("supplier-1", _resolver.Resolve("erp", "SUPPLIER", "SHARED-KEY"));
        Assert.Equal("facility-1", _resolver.Resolve("erp", "FACILITY", "SHARED-KEY"));
    }

    [Fact]
    public void DifferentSourceSystems_DoNotCollide()
    {
        _resolver.Register("erp", "SUPPLIER", "K1", "from-erp");
        _resolver.Register("wms", "SUPPLIER", "K1", "from-wms");

        Assert.Equal("from-erp", _resolver.Resolve("erp", "SUPPLIER", "K1"));
        Assert.Equal("from-wms", _resolver.Resolve("wms", "SUPPLIER", "K1"));
    }

    [Fact]
    public void Reregistering_OverwritesTheMapping()
    {
        _resolver.Register("erp", "SUPPLIER", "SUP-1", "first");
        _resolver.Register("erp", "SUPPLIER", "SUP-1", "second");

        Assert.Equal("second", _resolver.Resolve("erp", "SUPPLIER", "SUP-1"));
    }

    [Fact]
    public void Register_WithBlankCanonicalId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _resolver.Register("erp", "SUPPLIER", "SUP-1", "  "));
    }

    [Fact]
    public void ConcurrentRegisterAndResolve_IsSafe()
    {
        Parallel.For(0, 500, i =>
        {
            _resolver.Register("erp", "SUPPLIER", $"SUP-{i}", $"canon-{i}");
            _resolver.Resolve("erp", "SUPPLIER", $"SUP-{i}");
        });

        for (var i = 0; i < 500; i++)
        {
            Assert.Equal($"canon-{i}", _resolver.Resolve("erp", "SUPPLIER", $"SUP-{i}"));
        }
    }
}
