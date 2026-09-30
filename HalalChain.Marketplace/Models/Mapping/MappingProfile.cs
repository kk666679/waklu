using AutoMapper;
using HalalChain.Domain.Catalog;
using HalalChain.Domain.Commerce;
using HalalChain.Domain.Halal;
using HalalChain.Domain.Vendors;
using HalalChain.Marketplace.Models.ViewModels;

namespace HalalChain.Marketplace.Models.Mapping;

/// <summary>AutoMapper profile for mapping domain entities to ViewModels.</summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── PRODUCT ─────────────────────────────────────────────────────────
        CreateMap<Product, ProductViewModel>()
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(p => p.Vendor != null ? p.Vendor.Name : ""))
            .ForMember(vm => vm.BrandName, opt => opt.MapFrom(p => p.Brand != null ? p.Brand.Name : ""))
            .ForMember(vm => vm.CategoryName, opt => opt.MapFrom(p => p.Category != null ? p.Category.Name : ""))
            .ForMember(vm => vm.ProductTypeName, opt => opt.MapFrom(p => p.ProductType != null ? p.ProductType.Name : ""))
            .ForMember(vm => vm.MediaAssets, opt => opt.MapFrom(p => p.MediaAssets))
            .ForMember(vm => vm.Variants, opt => opt.MapFrom(p => p.Variants));

        CreateMap<ProductVariant, ProductVariantViewModel>();
        CreateMap<ProductMediaAsset, ProductMediaViewModel>();

        CreateMap<Product, ProductSummaryViewModel>()
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(p => p.Vendor != null ? p.Vendor.Name : ""))
            .ForMember(vm => vm.BrandName, opt => opt.MapFrom(p => p.Brand != null ? p.Brand.Name : ""))
            .ForMember(vm => vm.PrimaryImageUrl,
                opt => opt.MapFrom(p => PrimaryImageUrl(p.MediaAssets)));

        // ── VENDOR ──────────────────────────────────────────────────────────
        CreateMap<Vendor, VendorViewModel>();
        CreateMap<Vendor, VendorProfileViewModel>()
            .ForMember(vm => vm.FeaturedProducts, opt => opt.MapFrom(v => v.Products.Take(6)));

        // ── CART ────────────────────────────────────────────────────────────
        CreateMap<CartItem, CartItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(c => c.Product != null ? c.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl,
                opt => opt.MapFrom(c => PrimaryImageUrl(c.Product)))
            .ForMember(vm => vm.Price, opt => opt.MapFrom(c => c.Product != null ? c.Product.Price : 0))
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(c => VendorName(c.Product)));

        // ── WISHLIST ────────────────────────────────────────────────────────
        CreateMap<WishlistItem, WishlistItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(w => w.Product != null ? w.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl,
                opt => opt.MapFrom(w => PrimaryImageUrl(w.Product)))
            .ForMember(vm => vm.Price, opt => opt.MapFrom(w => w.Product != null ? w.Product.Price : 0))
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(w => VendorName(w.Product)));

        // ── ORDER ───────────────────────────────────────────────────────────
        CreateMap<Order, OrderViewModel>()
            .ForMember(vm => vm.VendorOrders, opt => opt.MapFrom(o => o.VendorOrders));

        CreateMap<VendorOrder, VendorOrderViewModel>()
            .ForMember(vm => vm.Items, opt => opt.MapFrom(vo => vo.Items));

        CreateMap<OrderItem, OrderItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(oi => oi.Product != null ? oi.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl,
                opt => opt.MapFrom(oi => PrimaryImageUrl(oi.Product)));

        CreateMap<Order, OrderHistoryViewModel>()
            .ForMember(vm => vm.ItemCount, opt => opt.MapFrom(o =>
                o.VendorOrders != null
                    ? o.VendorOrders.Sum(vo => vo.Items!.Sum(oi => oi.Quantity))
                    : 0));

        // ── HALAL VERIFICATION ──────────────────────────────────────────────
        // HalalVerification holds ProductId only — it has no Product navigation,
        // so the view model's ProductName is not derivable here.
        CreateMap<HalalVerification, HalalVerificationViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.Ignore())
            .ForMember(vm => vm.ComplianceStatus,
                opt => opt.MapFrom(v => v.ComplianceStatus.ToString()));

        CreateMap<Certificate, CertificateViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.Ignore())
            .ForMember(vm => vm.Status, opt => opt.MapFrom(c => c.Status.ToString()));

        // ── CATEGORIES ──────────────────────────────────────────────────────
        CreateMap<Department, DepartmentViewModel>()
            .ForMember(vm => vm.Categories, opt => opt.MapFrom(d => d.Categories));

        CreateMap<TaxonomyCategory, TaxonomyCategoryViewModel>()
            .ForMember(vm => vm.Subcategories, opt => opt.MapFrom(tc => tc.Subcategories));

        CreateMap<Subcategory, SubcategoryViewModel>()
            .ForMember(vm => vm.ProductTypes, opt => opt.MapFrom(s => s.ProductTypes));

        CreateMap<ProductType, ProductTypeViewModel>();

        CreateMap<Brand, BrandSummaryViewModel>();

        // Brand is its own aggregate with no Products collection, so the brand
        // detail view gets an empty product list rather than a broken mapping.
        CreateMap<Brand, BrandDetailViewModel>()
            .ForMember(vm => vm.Products,
                opt => opt.MapFrom(_ => new List<ProductViewModel>()));

        // ── CATEGORY BROWSE ─────────────────────────────────────────────────
        CreateMap<TaxonomyCategory, CategoryBrowseViewModel>()
            .ForMember(vm => vm.Products, opt => opt.MapFrom(tc => new List<ProductSummaryViewModel>()))
            .ForMember(vm => vm.Breadcrumbs, opt => opt.Ignore())
            .ForMember(vm => vm.Attributes, opt => opt.Ignore())
            .ForMember(vm => vm.Subcategories, opt => opt.MapFrom(tc => tc.Subcategories));
    }

    private static string? PrimaryImageUrl(Product? product) =>
        product is null ? null : PrimaryImageUrl(product.MediaAssets);

    private static string VendorName(Product? product) =>
        product?.Vendor?.Name ?? string.Empty;

    /// <summary>
    /// Picks the image a card should render: the asset flagged primary, else
    /// the first one. Null when the product carries no media.
    ///
    /// A method call rather than inline null propagation because AutoMapper
    /// builds an expression tree, and expression trees reject <c>?.</c>.
    /// </summary>
    private static string? PrimaryImageUrl(List<ProductMediaAsset>? assets)    {
        if (assets is null || assets.Count == 0)
            return null;

        foreach (var asset in assets)
        {
            if (asset.IsPrimary)
                return asset.Url;
        }

        return assets[0].Url;
    }
}
