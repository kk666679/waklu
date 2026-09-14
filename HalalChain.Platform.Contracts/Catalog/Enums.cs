namespace HalalChain.Platform.Contracts.Catalog;

/// <summary>
/// Trust signal indicating how a product's halal status was established.
/// This is the single most important filter in a halal marketplace —
/// it is NOT a category (a product does not "live" under a status),
/// it is an attribute the product carries.
/// </summary>
public enum HalalStatus
{
    /// <summary>Not yet assessed.</summary>
    Unknown = 0,

    /// <summary>Certified by a recognised third-party halal certification body (uploaded certificate).</summary>
    Certified = 1,

    /// <summary>HalalChain's Tawheed AI agent verified ingredients and supplier declarations.</summary>
    VerifiedByPlatform = 2,

    /// <summary>Muslim-friendly service or venue without a full certification (restaurants, hotels).</summary>
    MuslimFriendly = 3,

    /// <summary>Vendor self-declares halal compliance (lower trust; flagged in UI).</summary>
    SelfDeclared = 4,

    /// <summary>Under review by the platform trust team.</summary>
    Pending = 5,

    /// <summary>Not applicable (e.g. kitchenware, decor, non-consumables).</summary>
    NotApplicable = 6,
}

/// <summary>
/// Animal-derivative disclosure. Critical for F&amp;B, supplements, cosmetics.
/// </summary>
public enum AnimalDerivative
{
    None = 0,
    Beef = 1,
    Pork = 2,
    Poultry = 3,
    Fish = 4,
    Insect = 5,
    Unspecified = 6,
    PlantAlternative = 7,
}

/// <summary>Source of any gelatin in the product.</summary>
public enum GelatinSource
{
    None = 0,
    Pork = 1,
    BeefHalal = 2,
    BeefNonHalal = 3,
    Fish = 4,
    PlantAgar = 5,
    PlantPectin = 6,
    Undisclosed = 7,
}

/// <summary>Source of any enzymes (rennet, lipase, etc.) used in production.</summary>
public enum EnzymeSource
{
    Microbial = 0,
    Plant = 1,
    AnimalUnspecified = 2,
    AnimalHalalCertified = 3,
    None = 3,
}

/// <summary>Fermentation medium for products like kombucha, vinegar, probiotics.</summary>
public enum FermentationMedium
{
    NotApplicable = 0,
    PlantOnly = 1,
    AnimalDerived = 2,
    AlcoholProducing = 3,
    Undisclosed = 4,
}

/// <summary>
/// Dietary attributes. Flags enum so a product can carry many simultaneously.
/// `Halal` here means "contains no haram" without claiming certification
/// (use <see cref="HalalStatus"/> for the certification-level signal).
/// </summary>
[Flags]
public enum DietaryTags
{
    None = 0,
    Halal = 1 << 0,
    Vegetarian = 1 << 1,
    Vegan = 1 << 2,
    GlutenFree = 1 << 3,
    DairyFree = 1 << 4,
    NutFree = 1 << 5,
    SoyFree = 1 << 6,
    SugarFree = 1 << 7,
    LowSodium = 1 << 8,
    Organic = 1 << 9,
    NonGMO = 1 << 10,
    KetoFriendly = 1 << 11,
    PaleoFriendly = 1 << 12,
    Raw = 1 << 13,
    LowCarb = 1 << 14,
}

/// <summary>
/// Commercial channel — drives B2B vs B2C UX, pricing tiers, and MOQ.
/// </summary>
public enum CommercialChannel
{
    Retail = 0,
    Wholesale = 1,
    Both = 2,
}

/// <summary>Audience segments — drives filter chips and merchandising.</summary>
[Flags]
public enum AudienceSegment
{
    None = 0,
    Men = 1 << 0,
    Women = 1 << 1,
    Unisex = 1 << 2,
    Children = 1 << 3,
    Babies = 1 << 4,
    Toddlers = 1 << 5,
    Teens = 1 << 6,
    Seniors = 1 << 7,
    Pets = 1 << 8,
    Livestock = 1 << 9,
    Restaurants = 1 << 10,
    Hotels = 1 << 11,
    Caterers = 1 << 12,
    Manufacturers = 1 << 13,
    Retailers = 1 << 14,
    Families = 1 << 15,
}

/// <summary>Trust tier of a certification body. Drives UI badge colour.</summary>
public enum CertificationTrustTier
{
    Unverified = 0,
    Tier3_Emerging = 1,
    Tier2_Reputable = 2,
    Tier1_Government = 3,
    Tier1_International = 4,
}

/// <summary>Scope of a halal certificate.</summary>
public enum CertificationScope
{
    Product = 0,
    ProductLine = 1,
    Facility = 2,
    WholeSupplyChain = 3,
    SlaughterOnly = 4,
    ProcessingOnly = 5,
}
