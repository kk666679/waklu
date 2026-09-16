using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Commerce;
using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;
using HalalChain.Platform.Api.Persistence;
using CertificateStatus = HalalChain.Domain.Halal.CertificateStatus;
using ComplianceStatus = HalalChain.Domain.Halal.ComplianceStatus;

namespace HalalChain.Platform.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly HalalChainDbContext _db;
    private readonly string _dbName = Guid.NewGuid().ToString();

    public PersistenceTests()
    {
        var options = new DbContextOptionsBuilder<HalalChainDbContext>().UseInMemoryDatabase(_dbName).Options;
        _db = new HalalChainDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() { _db.Dispose(); }

    [Fact]
    public async Task Category_InsertAndQuery()
    {
        _db.Categories.Add(new Category { Name="Snacks", Slug="snacks-t1" });
        await _db.SaveChangesAsync();
        (await _db.Categories.FirstAsync(x=>x.Slug=="snacks-t1")).Name.Should().Be("Snacks");
    }


    [Fact]
    public async Task Category_ParentChild()
    {
        var p=new Category { Name="Food", Slug="food-pc" };
        _db.Categories.Add(p); await _db.SaveChangesAsync();
        _db.Categories.Add(new Category { Name="Snacks", Slug="snacks-pc", ParentId=p.Id });
        await _db.SaveChangesAsync();
        (await _db.Categories.Include(c=>c.Children).FirstAsync(c=>c.Slug=="food-pc")).Children.Should().HaveCount(1);
    }

    [Fact]
    public async Task Vendor_InsertAndQuery()
    {
        _db.Vendors.Add(new Vendor { Name="ABC", Slug="abc-t1", Country="MY", Status="Active" });
        await _db.SaveChangesAsync();
        (await _db.Vendors.FirstAsync(v=>v.Slug=="abc-t1")).Country.Should().Be("MY");
    }

    [Fact]
    public async Task Product_WithCategoryAndVendor()
    {
        var cat=new Category { Name="B", Slug="bev-t1" };
        var ven=new Vendor { Name="D", Slug="dr-t1" };
        _db.Categories.Add(cat); _db.Vendors.Add(ven); await _db.SaveChangesAsync();
        _db.Products.Add(new Product { Title="Coconut", Slug="coco-t1", CategoryId=cat.Id, VendorId=ven.Id, Price=12.5m, Currency="MYR", Inventory=100, Origin="MY" });
        await _db.SaveChangesAsync();
        var r=await _db.Products.Include(x=>x.Category).Include(x=>x.Vendor).FirstAsync(x=>x.Slug=="coco-t1");
        r.Category!.Name.Should().Be("B"); r.Vendor.Name.Should().Be("D");
    }

    [Fact]
    public async Task Certificate_ForProduct()
    {
        var cat=new Category { Name="C", Slug="ccert1" };
        var ven=new Vendor { Name="V", Slug="vcert1" };
        _db.Categories.Add(cat); _db.Vendors.Add(ven); await _db.SaveChangesAsync();
        var p=new Product { Title="P", Slug="pcert1", CategoryId=cat.Id, VendorId=ven.Id, Price=5m };
        _db.Products.Add(p); await _db.SaveChangesAsync();
        _db.Certificates.Add(new Certificate { ProductId=p.Id, CertificateNumber="JAKIM-2024-001", CertificationBody="JAKIM", Jurisdiction="MY", Status=CertificateStatus.Verified, IssueDate=DateTimeOffset.UtcNow, ExpiryDate=DateTimeOffset.UtcNow.AddYears(2) });
        await _db.SaveChangesAsync();
        (await _db.Certificates.FirstAsync(c=>c.CertificateNumber=="JAKIM-2024-001")).Status.Should().Be(CertificateStatus.Verified);
    }

    [Fact]
    public async Task Certificate_CascadeDeletes()
    {
        var cat=new Category { Name="C", Slug="ccasc1" };
        var ven=new Vendor { Name="V", Slug="vcasc1" };
        _db.Categories.Add(cat); _db.Vendors.Add(ven); await _db.SaveChangesAsync();
        var p=new Product { Title="P", Slug="pcasc1", CategoryId=cat.Id, VendorId=ven.Id, Price=5m };
        _db.Products.Add(p); await _db.SaveChangesAsync();
        _db.Certificates.Add(new Certificate { ProductId=p.Id, CertificateNumber="CERT1", CertificationBody="J", IssueDate=DateTimeOffset.UtcNow, ExpiryDate=DateTimeOffset.UtcNow.AddYears(1) });
        await _db.SaveChangesAsync();
        _db.Products.Remove(p); await _db.SaveChangesAsync();
        (await _db.Certificates.AnyAsync(c=>c.CertificateNumber=="CERT1")).Should().BeFalse();
    }

    [Fact]
    public async Task HalalVerification_WithEvidence()
    {
        var cat=new Category { Name="C", Slug="cver1" };
        var ven=new Vendor { Name="V", Slug="vver1" };
        _db.Categories.Add(cat); _db.Vendors.Add(ven); await _db.SaveChangesAsync();
        var p=new Product { Title="P", Slug="pver1", CategoryId=cat.Id, VendorId=ven.Id, Price=5m };
        _db.Products.Add(p); await _db.SaveChangesAsync();
        var v=new HalalVerification { ProductId=p.Id, ComplianceStatus=ComplianceStatus.Verified, PolicyVersion="MY-v3", Jurisdiction="MY" };
        _db.HalalVerifications.Add(v); await _db.SaveChangesAsync();
        _db.VerificationEvidence.Add(new VerificationEvidence { VerificationId=v.Id, EvidenceType="cert", Source="JAKIM", Confidence=0.95 });
        _db.VerificationAudits.Add(new VerificationAudit { VerificationId=v.Id, Action="ADDED", Actor="sys" });
        await _db.SaveChangesAsync();
        var l=await _db.HalalVerifications.Include(x=>x.Evidences).Include(x=>x.Audits).FirstAsync(x=>x.Id==v.Id);
        l.Evidences.Should().HaveCount(1); l.Audits.Should().HaveCount(1);
    }

    [Fact]
    public async Task Order_WithVendorOrders()
    {
        var cat=new Category { Name="C", Slug="cord1" };
        var ven=new Vendor { Name="V", Slug="vord1" };
        _db.Categories.Add(cat); _db.Vendors.Add(ven); await _db.SaveChangesAsync();
        var p=new Product { Title="P", Slug="pord1", CategoryId=cat.Id, VendorId=ven.Id, Price=25m, Currency="MYR" };
        _db.Products.Add(p); await _db.SaveChangesAsync();
        var o=new Order { CustomerId=Guid.NewGuid(), Status="Paid", Total=50m, Currency="MYR" };
        _db.Orders.Add(o); await _db.SaveChangesAsync();
        var vo=new VendorOrder { OrderId=o.Id, VendorId=ven.Id, Subtotal=50m, Currency="MYR" };
        _db.VendorOrders.Add(vo); await _db.SaveChangesAsync();
        _db.OrderItems.Add(new OrderItem { VendorOrderId=vo.Id, ProductId=p.Id, Quantity=2, UnitPrice=25m, Currency="MYR" });
        await _db.SaveChangesAsync();
        var l=await _db.Orders.Include(x=>x.VendorOrders).ThenInclude(x=>x.Items).ThenInclude(x=>x.Product).FirstAsync(x=>x.Id==o.Id);
        l.VendorOrders.Should().HaveCount(1); l.VendorOrders[0].Items[0].Product.Title.Should().Be("P");
    }


    [Fact]
    public async Task SeedData_HasExpectedEntities()
    {
        (await _db.Products.CountAsync()).Should().BeGreaterThanOrEqualTo(50);
        (await _db.Vendors.CountAsync()).Should().BeGreaterThan(0);
        (await _db.Categories.CountAsync()).Should().BeGreaterThan(0);
    }

}
