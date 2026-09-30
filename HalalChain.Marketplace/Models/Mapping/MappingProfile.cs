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
            .ForMember(vm => vm.PrimaryImageUrl, opt => opt.MapFrom(p => 
                p.MediaAssets != null && p.MediaAssets.Any(m => m.IsPrimary) 
                    ? p.MediaAssets.First(m => m.IsPrimary).Url 
                    : p.MediaAssets?.FirstOrDefault()?.Url));

        // ── VENDOR ──────────────────────────────────────────────────────────
        CreateMap<Vendor, VendorViewModel>();
        CreateMap<Vendor, VendorProfileViewModel>()
            .ForMember(vm => vm.FeaturedProducts, opt => opt.MapFrom(v => v.Products!.Take(6)));

        // ── CART ────────────────────────────────────────────────────────────
        CreateMap<CartItem, CartItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(c => c.Product != null ? c.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl, opt => opt.MapFrom(c => 
                c.Product?.MediaAssets?.FirstOrDefault(m => m.IsPrimary)?.Url 
                ?? c.Product?.MediaAssets?.FirstOrDefault()?.Url))
            .ForMember(vm => vm.Price, opt => opt.MapFrom(c => c.Product != null ? c.Product.Price : 0))
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(c => c.Product?.Vendor != null ? c.Product.Vendor.Name : ""));

        // ── WISHLIST ────────────────────────────────────────────────────────
        CreateMap<WishlistItem, WishlistItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(w => w.Product != null ? w.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl, opt => opt.MapFrom(w => 
                w.Product?.MediaAssets?.FirstOrDefault(m => m.IsPrimary)?.Url 
                ?? w.Product?.MediaAssets?.FirstOrDefault()?.Url))
            .ForMember(vm => vm.Price, opt => opt.MapFrom(w => w.Product != null ? w.Product.Price : 0))
            .ForMember(vm => vm.VendorName, opt => opt.MapFrom(w => w.Product?.Vendor != null ? w.Product.Vendor.Name : ""));

        // ── ORDER ───────────────────────────────────────────────────────────
        CreateMap<Order, OrderViewModel>()
            .ForMember(vm => vm.VendorOrders, opt => opt.MapFrom(o => o.VendorOrders));

        CreateMap<VendorOrder, VendorOrderViewModel>()
            .ForMember(vm => vm.Items, opt => opt.MapFrom(vo => vo.Items));

        CreateMap<OrderItem, OrderItemViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(oi => oi.Product != null ? oi.Product.Title : ""))
            .ForMember(vm => vm.ProductImageUrl, opt => opt.MapFrom(oi => 
                oi.Product?.MediaAssets?.FirstOrDefault(m => m.IsPrimary)?.Url 
                ?? oi.Product?.MediaAssets?.FirstOrDefault()?.Url));

        CreateMap<Order, OrderHistoryViewModel>()
            .ForMember(vm => vm.ItemCount, opt => opt.MapFrom(o => 
                o.VendorOrders != null ? o.VendorOrders.Sum(vo => vo.Items!.Sum(oi => oi.Quantity)) : 0));

        // ── HALAL VERIFICATION ──────────────────────────────────────────────
        CreateMap<HalalVerification, HalalVerificationViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(v => 
                v.Product != null ? v.Product.Title : ""));

        CreateMap<Certificate, CertificateViewModel>()
            .ForMember(vm => vm.ProductName, opt => opt.MapFrom(c => 
                c.Product != null ? c.Product.Title : ""));

        // ── CATEGORIES ──────────────────────────────────────────────────────
        CreateMap<Department, DepartmentViewModel>()
            .ForMember(vm => vm.Categories, opt => opt.MapFrom(d => d.Categories));

        CreateMap<TaxonomyCategory, TaxonomyCategoryViewModel>()
            .ForMember(vm => vm.Subcategories, opt => opt.MapFrom(tc => tc.Subcategories));

        CreateMap<Subcategory, SubcategoryViewModel>()
            .ForMember(vm => vm.ProductTypes, opt => opt.MapFrom(s => s.ProductTypes));

        CreateMap<ProductType, ProductTypeViewModel>();

        CreateMap<Brand, BrandSummaryViewModel>();
        CreateMap<Brand, BrandDetailViewModel>()
            .ForMember(vm => vm.Products, opt => opt.MapFrom(b => b.Products != null ? b.Products.Take(12) : new List<Product>()));

        // ── CATEGORY BROWSE ─────────────────────────────────────────────────
        CreateMap<TaxonomyCategory, CategoryBrowseViewModel>()
            .ForMember(vm => vm.Products, opt => opt.MapFrom(tc => new List<ProductSummaryViewModel>()))
            .ForMember(vm => vm.Breadcrumbs, opt => opt.Ignore())
            .ForMember(vm => vm.Attributes, opt => opt.Ignore())
            .ForMember(vm => vm.Subcategories, opt => opt.MapFrom(tc => tc.Subcategories));
    }
}
