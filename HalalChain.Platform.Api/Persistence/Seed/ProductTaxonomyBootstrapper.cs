using HalalChain.Platform.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Persistence.Seed;

/// <summary>
/// One-shot runtime bootstrapper that enriches the legacy seeded products
/// with the new taxonomy data: assigns each product a ProductTypeId by
/// resolving the right leaf in the 4-level taxonomy, and populates a
/// structured HalalProfile so the redesigned filter endpoint returns
/// meaningful results immediately after deployment.
///
/// Why runtime (not HasData)?
///  - The taxonomy seed uses deterministic-hash GUIDs for ProductTypes,
///    so we can't reference them from compile-time HasData literals.
///  - Resolving at startup lets us look up the real ProductType rows
///    by (department, category, subcategory, type) slug.
///  - It's idempotent: only updates products that haven't been enriched
///    yet (ProductTypeId IS NULL).
///  - It's safe to re-run: no-op once all products are enriched.
/// </summary>
public static class ProductTaxonomyBootstrapper
{
    /// <summary>The mapping from legacy product slug → the 4-level
    /// taxonomy path. Each entry tells the bootstrapper where the
    /// product belongs in the new tree.</summary>
    private static readonly Dictionary<string, (string Dept, string Cat, string Sub, string Type, HalalStatus Status, string Country, DietaryTags Dietary, AudienceSegment Segments, CommercialChannel Channel)> Map = new()
    {
        // ── Snacks & Confectionery ─────────────────────────────────
        ["keropok-lekor"]        = ("food-beverage", "snacks-confectionery", "chips",         "potato",       HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.GlutenFree, AudienceSegment.Families, CommercialChannel.Retail),
        ["keropok-spicy"]        = ("food-beverage", "snacks-confectionery", "chips",         "potato",       HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["murukku"]              = ("food-beverage", "snacks-confectionery", "chips",         "vegetable",    HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["kueh-lapis"]           = ("food-beverage", "bakery",               "traditional-desserts", "kueh",  HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["tempeh-chips"]         = ("food-beverage", "snacks-confectionery", "chips",         "vegetable",    HalalStatus.Certified, "ID", DietaryTags.Halal | DietaryTags.Vegan | DietaryTags.NonGMO, AudienceSegment.Families, CommercialChannel.Retail),
        ["rengginang"]           = ("food-beverage", "snacks-confectionery", "chips",         "vegetable",    HalalStatus.Certified, "ID", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["tau-huay"]             = ("food-beverage", "snacks-confectionery", "chips",         "vegetable",    HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["baklava"]              = ("food-beverage", "bakery",               "traditional-desserts", "baklava",  HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["namkeen"]              = ("food-beverage", "snacks-confectionery", "nuts-seeds",    "mixed-nuts",   HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["peanut-brittle"]       = ("food-beverage", "snacks-confectionery", "nuts-seeds",    "mixed-nuts",   HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),

        // ── Beverages ─────────────────────────────────────────────
        ["teh-tarik"]            = ("food-beverage", "beverages", "tea",         "instant",     HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["kopi-o"]               = ("food-beverage", "beverages", "coffee",      "instant",     HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["sirap-bandung"]        = ("food-beverage", "beverages", "soft-drinks", "tonic",       HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["kunyit-asam"]          = ("food-beverage", "beverages", "tea",         "herbal",      HalalStatus.Certified, "ID", DietaryTags.Halal | DietaryTags.Vegan | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
        ["coconut-water"]        = ("food-beverage", "beverages", "water",       "coconut-water",HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["qahwa"]               = ("food-beverage", "beverages", "coffee",      "arabic",      HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["rose-water"]          = ("food-beverage", "beverages", "soft-drinks", "lemonade",    HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["bandung"]             = ("food-beverage", "beverages", "soft-drinks", "soda",        HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),

        // ── Meat & Poultry ─────────────────────────────────────────
        ["ayam-kampung"]         = ("food-beverage", "meat-poultry", "chicken",   "whole",       HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["daging-kambing"]       = ("food-beverage", "meat-poultry", "goat",      "cuts",        HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["sate-ayam"]            = ("food-beverage", "meat-poultry", "chicken",   "marinated",   HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["rendang-daging"]       = ("food-beverage", "meat-poultry", "beef",      "frozen",      HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["chicken-nugget"]       = ("food-beverage", "meat-poultry", "chicken",   "frozen",      HalalStatus.Certified, "SG", DietaryTags.Halal, AudienceSegment.Children | AudienceSegment.Families, CommercialChannel.Retail),
        ["lamb-mince"]           = ("food-beverage", "meat-poultry", "lamb-mutton", "minced",    HalalStatus.Certified, "AE", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["sosis-sapi"]           = ("food-beverage", "meat-poultry", "beef",      "sausages",    HalalStatus.Certified, "ID", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["otak-otak"]            = ("food-beverage", "seafood",      "fish",      "smoked",      HalalStatus.Certified, "MY", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),

        // ── Sauces & Spices ────────────────────────────────────────
        ["rendang-paste"]        = ("food-beverage", "sauces-spices", "sauces",    "sambal",      HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["sambal-terasi"]        = ("food-beverage", "sauces-spices", "sauces",    "sambal",      HalalStatus.Certified, "ID", DietaryTags.Halal, AudienceSegment.Families, CommercialChannel.Retail),
        ["kari-ayam"]            = ("food-beverage", "sauces-spices", "spices",    "curry-powder",HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["turmeric"]            = ("food-beverage", "sauces-spices", "spices",    "turmeric",    HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
        ["zaatar"]               = ("food-beverage", "sauces-spices", "spices",    "zaatar",      HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
        ["black-pepper"]         = ("food-beverage", "sauces-spices", "spices",    "black-pepper",HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["curry-leaf"]           = ("food-beverage", "sauces-spices", "spices",    "cardamom",    HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
        ["bumbu-nasi"]           = ("food-beverage", "sauces-spices", "sauces",    "marinade",    HalalStatus.Certified, "ID", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),

        // ── Bakery ─────────────────────────────────────────────────
        ["roti-canai"]           = ("food-beverage", "bakery", "bread",  "flatbread",   HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["naan-garlic"]          = ("food-beverage", "bakery", "bread",  "flatbread",   HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["wholemeal-bread"]      = ("food-beverage", "bakery", "bread",  "sliced",      HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["kuih-bahulu"]          = ("food-beverage", "bakery", "cakes",  "cupcakes",    HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["muffin"]               = ("food-beverage", "bakery", "cakes",  "cupcakes",    HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["date-cookies"]         = ("food-beverage", "snacks-confectionery", "biscuits", "filled", HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["pia-durian"]           = ("food-beverage", "bakery", "pastries","pastries",  HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),
        ["martabak"]             = ("food-beverage", "bakery", "pastries","pastries",  HalalStatus.Certified, "ID", DietaryTags.Halal | DietaryTags.Vegetarian, AudienceSegment.Families, CommercialChannel.Retail),

        // ── Personal Care ──────────────────────────────────────────
        ["vitamin-c"]            = ("personal-care", "skincare", "facial", "serums",      HalalStatus.Certified, "SG", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),
        ["spirulina-mask"]       = ("personal-care", "skincare", "facial", "face-masks",  HalalStatus.Certified, "ID", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),
        ["aloe-moisturizer"]     = ("personal-care", "skincare", "facial", "moisturizers",HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),
        ["rose-toner"]           = ("personal-care", "skincare", "facial", "toners",      HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),
        ["palm-shampoo"]         = ("personal-care", "haircare", "shampoo-conditioner", "shampoo",  HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Unisex, CommercialChannel.Retail),
        ["argan-oil"]            = ("personal-care", "haircare", "treatments-styling", "hair-oils", HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),
        ["hijab-cond"]           = ("personal-care", "haircare", "shampoo-conditioner", "conditioner",  HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Women, CommercialChannel.Retail),

        // ── Health ──────────────────────────────────────────────────
        ["tualang-honey"]        = ("health-wellness", "herbal-traditional", "traditional", "honey",         HalalStatus.Certified, "MY", DietaryTags.Halal | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
        ["habbatus-sauda"]       = ("health-wellness", "herbal-traditional", "herbal-supps", "habbatus-sauda", HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Vegan, AudienceSegment.Families, CommercialChannel.Retail),
        ["medjool-dates"]        = ("health-wellness", "herbal-traditional", "traditional", "honey",         HalalStatus.Certified, "AE", DietaryTags.Halal | DietaryTags.Organic, AudienceSegment.Families, CommercialChannel.Retail),
    };

    /// <summary>
    /// Enrich all legacy products that don't yet have a ProductTypeId:
    /// resolve the right leaf in the 4-level taxonomy and write
    /// ProductTypeId + a structured HalalProfile.
    /// Returns the number of products enriched.
    /// </summary>
    public static async Task<int> EnrichAsync(HalalChainDbContext db, CancellationToken ct = default)
    {
        // Cache: (dept, cat, sub, type) → ProductType row. Built lazily
        // from the DB on first miss so we don't need to load the whole
        // taxonomy up-front.
        var cache = new Dictionary<(string, string, string, string), Guid?>();

        async Task<Guid?> ResolveAsync(string dept, string cat, string sub, string type, CancellationToken c)
        {
            var key = (dept, cat, sub, type);
            if (cache.TryGetValue(key, out var hit)) return hit;

            var id = await db.ProductTypes
                .AsNoTracking()
                .Where(t => t.Slug == type
                         && t.Subcategory.Slug == sub
                         && t.Subcategory.Category.Slug == cat
                         && t.Subcategory.Category.Department.Slug == dept)
                .Select(t => (Guid?)t.Id)
                .FirstOrDefaultAsync(c);
            cache[key] = id;
            return id;
        }

        var enriched = 0;
        var products = await db.Products
            .Where(p => p.ProductTypeId == null)
            .ToListAsync(ct);

        foreach (var p in products)
        {
            if (!Map.TryGetValue(p.Slug, out var m)) continue;

            var typeId = await ResolveAsync(m.Dept, m.Cat, m.Sub, m.Type, ct);
            if (typeId is null) continue;

            p.ProductTypeId = typeId;
            p.HalalProfile = new HalalProfile
            {
                Status              = m.Status,
                CountryOfOrigin     = m.Country,
                CountryOfManufacture = m.Country,
                Dietary             = m.Dietary,
                Channel             = m.Channel,
                Segments            = m.Segments,
                Tags                = new List<string> { m.Dept, m.Cat, m.Sub, m.Type },
                Allergens           = new List<string>(),
            };
            enriched++;
        }

        if (enriched > 0)
            await db.SaveChangesAsync(ct);

        return enriched;
    }
}
