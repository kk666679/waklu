namespace HalalChain.Domain.Catalog;

public enum HalalStatus
{
    Unknown = 0,
    Certified = 1,
    VerifiedByPlatform = 2,
    MuslimFriendly = 3,
    SelfDeclared = 4,
    Pending = 5,
    NotApplicable = 6,
}

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

public enum EnzymeSource
{
    Microbial = 0,
    Plant = 1,
    AnimalUnspecified = 2,
    AnimalHalalCertified = 3,
    None = 3,
}

public enum FermentationMedium
{
    NotApplicable = 0,
    PlantOnly = 1,
    AnimalDerived = 2,
    AlcoholProducing = 3,
    Undisclosed = 4,
}

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

public enum CommercialChannel
{
    Retail = 0,
    Wholesale = 1,
    Both = 2,
}

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

public enum CertificationTrustTier
{
    Unverified = 0,
    Tier3_Emerging = 1,
    Tier2_Reputable = 2,
    Tier1_Government = 3,
    Tier1_International = 4,
}

public enum CertificationScope
{
    Product = 0,
    ProductLine = 1,
    Facility = 2,
    WholeSupplyChain = 3,
    SlaughterOnly = 4,
    ProcessingOnly = 5,
}
