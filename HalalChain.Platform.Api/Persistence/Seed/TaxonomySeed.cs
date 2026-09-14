using HalalChain.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Persistence.Seed;

/// <summary>
/// Comprehensive seed for the redesigned HalalChain taxonomy:
/// 8 departments → ~70 categories → ~250 subcategories → ~500 product types,
/// plus countries, certification bodies, brands, and a remap of the
/// existing 50 legacy products into the new tree with structured HalalProfiles.
/// </summary>
public static class TaxonomySeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedCountries(modelBuilder);
        SeedCertificationBodies(modelBuilder);
        SeedBrands(modelBuilder);
        SeedDepartmentsCategoriesAndTypes(modelBuilder);
    }

    // ── Countries (ASEAN + key halal markets) ───────────────────────
    private static void SeedCountries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>().HasData(
            new Country { Iso2 = "MY", Iso3 = "MYS", Name = "Malaysia",        Region = "Southeast Asia" },
            new Country { Iso2 = "ID", Iso3 = "IDN", Name = "Indonesia",       Region = "Southeast Asia" },
            new Country { Iso2 = "SG", Iso3 = "SGP", Name = "Singapore",       Region = "Southeast Asia" },
            new Country { Iso2 = "BN", Iso3 = "BRN", Name = "Brunei",          Region = "Southeast Asia" },
            new Country { Iso2 = "TH", Iso3 = "THA", Name = "Thailand",        Region = "Southeast Asia" },
            new Country { Iso2 = "PH", Iso3 = "PHL", Name = "Philippines",     Region = "Southeast Asia" },
            new Country { Iso2 = "VN", Iso3 = "VNM", Name = "Vietnam",         Region = "Southeast Asia" },
            new Country { Iso2 = "AE", Iso3 = "ARE", Name = "United Arab Emirates", Region = "Middle East" },
            new Country { Iso2 = "SA", Iso3 = "SAU", Name = "Saudi Arabia",    Region = "Middle East" },
            new Country { Iso2 = "QA", Iso3 = "QAT", Name = "Qatar",           Region = "Middle East" },
            new Country { Iso2 = "KW", Iso3 = "KWT", Name = "Kuwait",          Region = "Middle East" },
            new Country { Iso2 = "BH", Iso3 = "BHR", Name = "Bahrain",         Region = "Middle East" },
            new Country { Iso2 = "OM", Iso3 = "OMN", Name = "Oman",            Region = "Middle East" },
            new Country { Iso2 = "TR", Iso3 = "TUR", Name = "Türkiye",         Region = "Europe" },
            new Country { Iso2 = "PK", Iso3 = "PAK", Name = "Pakistan",        Region = "South Asia" },
            new Country { Iso2 = "BD", Iso3 = "BGD", Name = "Bangladesh",      Region = "South Asia" },
            new Country { Iso2 = "GB", Iso3 = "GBR", Name = "United Kingdom",  Region = "Europe" },
            new Country { Iso2 = "US", Iso3 = "USA", Name = "United States",   Region = "North America" }
        );
    }

    // ── Certification bodies (the most recognised globally) ─────────
    private static void SeedCertificationBodies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificationBody>().HasData(
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000001"), Name = "Department of Islamic Development Malaysia (JAKIM)", Slug = "jakim",  Acronym = "JAKIM", Country = "MY", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "Malaysia's federal halal authority; widely recognised across ASEAN, MENA, and East Asia." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000002"), Name = "Majelis Ulama Indonesia (MUI)",                                  Slug = "mui",    Acronym = "MUI",   Country = "ID", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "Indonesia's national halal authority; largest Muslim population globally." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000003"), Name = "Emirates Authority for Standardisation (ESMA)",                   Slug = "esma",   Acronym = "ESMA",  Country = "AE", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "UAE federal authority; recognised across the Gulf and increasingly internationally." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000004"), Name = "Saudi Food and Drug Authority (SFDA)",                            Slug = "sfda",   Acronym = "SFDA",  Country = "SA", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "Saudi Arabia's federal authority; the most widely accepted standard in MENA." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000005"), Name = "Gulf Accreditation Center (GAC)",                                  Slug = "gac",    Acronym = "GAC",   Country = "AE", TrustTier = CertificationTrustTier.Tier1_International,  Description = "GCC-wide accreditation body for halal conformity assessment." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000006"), Name = "Singapore MUIS",                                                  Slug = "muis",   Acronym = "MUIS",  Country = "SG", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "Majlis Ugama Islam Singapura; Singapore's official halal authority." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000007"), Name = "Brunei Halal Brand",                                              Slug = "brunei-halal", Acronym = "BHB", Country = "BN", TrustTier = CertificationTrustTier.Tier1_Government,    Description = "Brunei's national halal certification, run under the Ministry of Religious Affairs." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000008"), Name = "Thailand Central Islamic Council of Thailand (CICOT)",            Slug = "cicot",  Acronym = "CICOT", Country = "TH", TrustTier = CertificationTrustTier.Tier2_Reputable,     Description = "Thailand's recognised halal certifier." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-000000000009"), Name = "Pakistan Halal Authority",                                        Slug = "pha",    Acronym = "PHA",   Country = "PK", TrustTier = CertificationTrustTier.Tier2_Reputable,     Description = "Pakistan's federal halal authority." },
            new CertificationBody { Id = Guid.Parse("10000010-0000-0000-0000-00000000000A"), Name = "Turkish Standards Institute (TSE)",                                Slug = "tse",    Acronym = "TSE",   Country = "TR", TrustTier = CertificationTrustTier.Tier2_Reputable,     Description = "Türkiye's national standards body; halal certification arm." }
        );
    }

    // ── Brands (the existing vendors get branded for the new model) ───
    private static void SeedBrands(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>().HasData(
            new Brand { Id = Guid.Parse("10000020-0000-0000-0000-000000000001"), Name = "Selera Masak",       Slug = "selera-masak",    CountryOfOrigin = "MY" },
            new Brand { Id = Guid.Parse("10000020-0000-0000-0000-000000000002"), Name = "Al-Barakah Foods",   Slug = "al-barakah",      CountryOfOrigin = "MY" },
            new Brand { Id = Guid.Parse("10000020-0000-0000-0000-000000000003"), Name = "Nusantara Herbal",   Slug = "nusantara",       CountryOfOrigin = "ID" },
            new Brand { Id = Guid.Parse("10000020-0000-0000-0000-000000000004"), Name = "PureBite SG",        Slug = "purebite",        CountryOfOrigin = "SG" },
            new Brand { Id = Guid.Parse("10000020-0000-0000-0000-000000000005"), Name = "Gulf Halal Trading", Slug = "gulf-halal",      CountryOfOrigin = "AE" }
        );
    }

    // ── The 8-department taxonomy ────────────────────────────────────
    // We use stable GUIDs so the seed is repeatable and migrations work.
    // Department → Category → Subcategory → ProductType, with PathSlug
    // pre-computed for URL stability.
    private static void SeedDepartmentsCategoriesAndTypes(ModelBuilder modelBuilder)
    {
        // IDs are generated deterministically from a base + offset per department.
        // Format: 0xDE0000DD where DE=department index (1..8), DD=node index.
        var seed = new TaxonomyBuilder(modelBuilder);

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 1 — Food & Beverage                            ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d1 = seed.AddDepartment("Food & Beverage", "food-beverage", "fa-utensils", "Halal-certified food and drink across all categories.", 1);

        // 1.1 Meat & Poultry
        var c1 = seed.AddCategory(d1, "Meat & Poultry", "meat-poultry", "fa-drumstick-bite", 1);
        var s1 = seed.AddSubcategory(c1, "Beef",         "beef");
        seed.AddType(s1, "Fresh cuts",   "fresh-cuts");
        seed.AddType(s1, "Minced",       "minced");
        seed.AddType(s1, "Frozen",       "frozen");
        seed.AddType(s1, "Marinated",    "marinated");
        seed.AddType(s1, "Whole / carcase","whole-carcase");
        var s2 = seed.AddSubcategory(c1, "Lamb & Mutton","lamb-mutton");
        seed.AddType(s2, "Fresh cuts",   "fresh-cuts");
        seed.AddType(s2, "Cubed",        "cubed");
        seed.AddType(s2, "Minced",       "minced");
        seed.AddType(s2, "On the bone",  "on-the-bone");
        var s3 = seed.AddSubcategory(c1, "Goat",         "goat");
        seed.AddType(s3, "Whole",        "whole");
        seed.AddType(s3, "Cuts",         "cuts");
        seed.AddType(s3, "Minced",       "minced");
        var s4 = seed.AddSubcategory(c1, "Chicken",      "chicken");
        seed.AddType(s4, "Whole",        "whole");
        seed.AddType(s4, "Cuts",         "cuts");
        seed.AddType(s4, "Frozen",       "frozen");
        seed.AddType(s4, "Marinated",    "marinated");
        seed.AddType(s4, "Organic / free-range","organic-free-range");
        var s5 = seed.AddSubcategory(c1, "Turkey",       "turkey");
        seed.AddType(s5, "Whole",        "whole");
        seed.AddType(s5, "Cuts",         "cuts");
        seed.AddType(s5, "Minced",       "minced");
        var s6 = seed.AddSubcategory(c1, "Duck",         "duck");
        seed.AddType(s6, "Whole",        "whole");
        seed.AddType(s6, "Cuts",         "cuts");
        var s7 = seed.AddSubcategory(c1, "Processed meat","processed-meat");
        seed.AddType(s7, "Sausages",     "sausages");
        seed.AddType(s7, "Burgers & patties","burgers-patties");
        seed.AddType(s7, "Deli meat",    "deli-meat");
        seed.AddType(s7, "Canned meat",  "canned-meat");
        seed.AddType(s7, "Jerky",        "jerky");

        // 1.2 Seafood
        var c2 = seed.AddCategory(d1, "Seafood", "seafood", "fa-fish", 2);
        var s8 = seed.AddSubcategory(c2, "Fish", "fish");
        seed.AddType(s8, "Whole fish",   "whole-fish");
        seed.AddType(s8, "Fillet",       "fillet");
        seed.AddType(s8, "Steaks",       "steaks");
        seed.AddType(s8, "Smoked",       "smoked");
        seed.AddType(s8, "Canned",       "canned");
        seed.AddType(s8, "Frozen",       "frozen");
        var s9 = seed.AddSubcategory(c2, "Prawns & Shrimp", "prawns-shrimp");
        seed.AddType(s9, "Fresh",        "fresh");
        seed.AddType(s9, "Frozen",       "frozen");
        seed.AddType(s9, "Peeled",       "peeled");
        seed.AddType(s9, "Cooked",       "cooked");
        var s10 = seed.AddSubcategory(c2, "Crab", "crab");
        seed.AddType(s10, "Whole",       "whole");
        seed.AddType(s10, "Pieces",      "pieces");
        seed.AddType(s10, "Frozen",      "frozen");
        var s11 = seed.AddSubcategory(c2, "Lobster", "lobster");
        seed.AddType(s11, "Whole",       "whole");
        seed.AddType(s11, "Tails",       "tails");
        var s12 = seed.AddSubcategory(c2, "Squid & Octopus", "squid-octopus");
        seed.AddType(s12, "Fresh",       "fresh");
        seed.AddType(s12, "Frozen",      "frozen");
        seed.AddType(s12, "Rings",       "rings");
        var s13 = seed.AddSubcategory(c2, "Shellfish", "shellfish");
        seed.AddType(s13, "Mussels",     "mussels");
        seed.AddType(s13, "Oysters",     "oysters");
        seed.AddType(s13, "Clams",       "clams");
        seed.AddType(s13, "Scallops",    "scallops");
        var s14 = seed.AddSubcategory(c2, "Canned & preserved seafood", "canned-preserved");
        seed.AddType(s14, "Canned fish", "canned-fish");
        seed.AddType(s14, "Canned shellfish","canned-shellfish");
        seed.AddType(s14, "Pastes & spreads","pastes-spreads");

        // 1.3 Dairy & Eggs
        var c3 = seed.AddCategory(d1, "Dairy & Eggs", "dairy-eggs", "fa-egg", 3);
        var s15 = seed.AddSubcategory(c3, "Milk", "milk");
        seed.AddType(s15, "Fresh",       "fresh");
        seed.AddType(s15, "UHT / long-life","uht");
        seed.AddType(s15, "Powdered",    "powdered");
        seed.AddType(s15, "Flavoured",   "flavoured");
        seed.AddType(s15, "Lactose-free","lactose-free");
        var s16 = seed.AddSubcategory(c3, "Cheese", "cheese");
        seed.AddType(s16, "Fresh",       "fresh");
        seed.AddType(s16, "Aged",        "aged");
        seed.AddType(s16, "Halloumi",    "halloumi");
        seed.AddType(s16, "Mozzarella",  "mozzarella");
        seed.AddType(s16, "Vegan",       "vegan");
        var s17 = seed.AddSubcategory(c3, "Yogurt & Yogurt drinks", "yogurt");
        seed.AddType(s17, "Plain",       "plain");
        seed.AddType(s17, "Flavoured",   "flavoured");
        seed.AddType(s17, "Greek",       "greek");
        seed.AddType(s17, "Drinkable",   "drinkable");
        var s18 = seed.AddSubcategory(c3, "Butter & Ghee", "butter-ghee");
        seed.AddType(s18, "Butter",      "butter");
        seed.AddType(s18, "Ghee",        "ghee");
        seed.AddType(s18, "Cultured",    "cultured");
        var s19 = seed.AddSubcategory(c3, "Cream & Crème fraîche", "cream");
        seed.AddType(s19, "Single",      "single");
        seed.AddType(s19, "Double",      "double");
        seed.AddType(s19, "Sour",        "sour");
        seed.AddType(s19, "Whipped",     "w whipped");
        var s20 = seed.AddSubcategory(c3, "Ice cream & Frozen desserts", "ice-cream");
        seed.AddType(s20, "Ice cream tubs","tubs");
        seed.AddType(s20, "Ice cream bars","bars");
        seed.AddType(s20, "Sorbet",      "sorbet");
        seed.AddType(s20, "Vegan",       "vegan");
        var s21 = seed.AddSubcategory(c3, "Milk alternatives", "milk-alt");
        seed.AddType(s21, "Oat",         "oat");
        seed.AddType(s21, "Almond",      "almond");
        seed.AddType(s21, "Soy",         "soy");
        seed.AddType(s21, "Coconut",     "coconut");
        var s22 = seed.AddSubcategory(c3, "Eggs", "eggs");
        seed.AddType(s22, "Chicken",     "chicken");
        seed.AddType(s22, "Duck",        "duck");
        seed.AddType(s22, "Quail",       "quail");
        seed.AddType(s22, "Organic",     "organic");
        var s23 = seed.AddSubcategory(c3, "Dairy desserts & puddings", "dairy-desserts");
        seed.AddType(s23, "Panna cotta", "panna-cotta");
        seed.AddType(s23, "Mousse",      "mousse");
        seed.AddType(s23, "Custard",     "custard");

        // 1.4 Rice, Grains & Staples
        var c4 = seed.AddCategory(d1, "Rice, Grains & Staples", "rice-grains-staples", "fa-wheat-awn", 4);
        var s24 = seed.AddSubcategory(c4, "Rice", "rice");
        seed.AddType(s24, "Basmati",     "basmati");
        seed.AddType(s24, "Jasmine",     "jasmine");
        seed.AddType(s24, "Brown",       "brown");
        seed.AddType(s24, "Glutinous",   "glutinous");
        seed.AddType(s24, "Basmati & brown blends","blends");
        var s25 = seed.AddSubcategory(c4, "Wheat & Flour", "wheat-flour");
        seed.AddType(s25, "All-purpose", "all-purpose");
        seed.AddType(s25, "Whole wheat", "whole-wheat");
        seed.AddType(s25, "Atta",        "atta");
        seed.AddType(s25, "Self-raising","self-raising");
        seed.AddType(s25, "Gluten-free", "gluten-free");
        var s26 = seed.AddSubcategory(c4, "Oats, Barley & Ancient grains", "oats-grains");
        seed.AddType(s26, "Rolled oats", "rolled-oats");
        seed.AddType(s26, "Steel-cut",   "steel-cut");
        seed.AddType(s26, "Barley",      "barley");
        seed.AddType(s26, "Quinoa",      "quinoa");
        seed.AddType(s26, "Buckwheat",   "buckwheat");
        var s27 = seed.AddSubcategory(c4, "Pasta & Noodles", "pasta-noodles");
        seed.AddType(s27, "Spaghetti",   "spaghetti");
        seed.AddType(s27, "Penne",       "penne");
        seed.AddType(s27, "Fusilli",     "fusilli");
        seed.AddType(s27, "Lasagne",     "lasagne");
        seed.AddType(s27, "Egg noodles", "egg-noodles");
        seed.AddType(s27, "Rice vermicelli","rice-vermicelli");
        seed.AddType(s27, "Udon",        "udon");
        var s28 = seed.AddSubcategory(c4, "Cereals & Muesli", "cereals");
        seed.AddType(s28, "Cornflakes",  "cornflakes");
        seed.AddType(s28, "Bran",        "bran");
        seed.AddType(s28, "Granola",     "granola");
        seed.AddType(s28, "Muesli",      "muesli");
        var s29 = seed.AddSubcategory(c4, "Bread, wraps & tortillas", "bread-wraps");
        seed.AddType(s29, "Flatbread",   "flatbread");
        seed.AddType(s29, "Pita",        "pita");
        seed.AddType(s29, "Tortilla",    "tortilla");
        seed.AddType(s29, "Lavash",      "lavash");

        // 1.5 Fresh Produce
        var c5 = seed.AddCategory(d1, "Fresh Produce", "fresh-produce", "fa-apple-whole", 5);
        var s30 = seed.AddSubcategory(c5, "Fresh fruits", "fresh-fruits");
        seed.AddType(s30, "Tropical",    "tropical");
        seed.AddType(s30, "Berries",     "berries");
        seed.AddType(s30, "Citrus",      "citrus");
        seed.AddType(s30, "Pome",        "pome");
        seed.AddType(s30, "Stone",       "stone");
        var s31 = seed.AddSubcategory(c5, "Fresh vegetables", "fresh-vegetables");
        seed.AddType(s31, "Leafy greens","leafy-greens");
        seed.AddType(s31, "Roots & tubers","roots-tubers");
        seed.AddType(s31, "Cruciferous", "cruciferous");
        seed.AddType(s31, "Alliums",     "alliums");
        seed.AddType(s31, "Peppers & chillies","peppers-chillies");
        seed.AddType(s31, "Tomatoes",    "tomatoes");
        seed.AddType(s31, "Cucumbers & squash","cucumbers-squash");
        seed.AddType(s31, "Pods & beans","pods-beans");
        var s32 = seed.AddSubcategory(c5, "Fresh herbs", "fresh-herbs");
        seed.AddType(s32, "Basil",       "basil");
        seed.AddType(s32, "Coriander",   "coriander");
        seed.AddType(s32, "Mint",        "mint");
        seed.AddType(s32, "Parsley",     "parsley");
        seed.AddType(s32, "Thyme",       "thyme");
        seed.AddType(s32, "Lemongrass",  "lemongrass");
        var s33 = seed.AddSubcategory(c5, "Mushrooms", "mushrooms");
        seed.AddType(s33, "Button",      "button");
        seed.AddType(s33, "Shiitake",    "shiitake");
        seed.AddType(s33, "Oyster",      "oyster");
        seed.AddType(s33, "Dried",       "dried");
        var s34 = seed.AddSubcategory(c5, "Dates & dried fruits", "dates-dried");
        seed.AddType(s34, "Medjool",     "medjool");
        seed.AddType(s34, "Ajwa",        "ajwa");
        seed.AddType(s34, "Deglet Noor", "deglet-noor");
        seed.AddType(s34, "Dried apricots","dried-apricots");
        seed.AddType(s34, "Dried figs",  "dried-figs");
        var s35 = seed.AddSubcategory(c5, "Fresh-cut & pre-prepared", "fresh-cut");
        seed.AddType(s35, "Salad mixes", "salad-mixes");
        seed.AddType(s35, "Cut fruit",   "cut-fruit");
        seed.AddType(s35, "Cut vegetables","cut-vegetables");

        // 1.6 Frozen Food
        var c6 = seed.AddCategory(d1, "Frozen Food", "frozen-food", "fa-snowflake", 6);
        var s36 = seed.AddSubcategory(c6, "Frozen meat", "frozen-meat");
        seed.AddType(s36, "Beef",        "beef");
        seed.AddType(s36, "Chicken",     "chicken");
        seed.AddType(s36, "Lamb",        "lamb");
        seed.AddType(s36, "Mixed",       "mixed");
        var s37 = seed.AddSubcategory(c6, "Frozen poultry", "frozen-poultry");
        seed.AddType(s37, "Whole",       "whole");
        seed.AddType(s37, "Cuts",        "cuts");
        var s38 = seed.AddSubcategory(c6, "Frozen seafood", "frozen-seafood");
        seed.AddType(s38, "Fillets",     "fillets");
        seed.AddType(s38, "Prawns",      "prawns");
        seed.AddType(s38, "Mixed",       "mixed");
        var s39 = seed.AddSubcategory(c6, "Frozen vegetables", "frozen-veg");
        seed.AddType(s39, "Mixed veg",   "mixed-veg");
        seed.AddType(s39, "Peas",        "peas");
        seed.AddType(s39, "Spinach",     "spinach");
        var s40 = seed.AddSubcategory(c6, "Frozen fruits", "frozen-fruit");
        seed.AddType(s40, "Berries",     "berries");
        seed.AddType(s40, "Mango",       "mango");
        seed.AddType(s40, "Mixed",       "mixed");
        var s41 = seed.AddSubcategory(c6, "Frozen ready meals", "frozen-meals");
        seed.AddType(s41, "Curry",       "curry");
        seed.AddType(s41, "Pasta",       "pasta");
        seed.AddType(s41, "Rice dishes", "rice-dishes");
        seed.AddType(s41, "Asian",       "asian");
        var s42 = seed.AddSubcategory(c6, "Frozen snacks", "frozen-snacks");
        seed.AddType(s42, "Samosa",      "samosa");
        seed.AddType(s42, "Spring roll", "spring-roll");
        seed.AddType(s42, "Puff",        "puff");
        var s43 = seed.AddSubcategory(c6, "Frozen desserts", "frozen-desserts");
        seed.AddType(s43, "Ice cream",   "ice-cream");
        seed.AddType(s43, "Cake",        "cake");
        seed.AddType(s43, "Pastry",      "pastry");

        // 1.7 Ready-to-Eat & Prepared
        var c7 = seed.AddCategory(d1, "Ready-to-Eat & Prepared", "ready-to-eat", "fa-bowl-food", 7);
        var s44 = seed.AddSubcategory(c7, "Ready meals", "ready-meals");
        seed.AddType(s44, "Rice",        "rice");
        seed.AddType(s44, "Noodle",      "noodle");
        seed.AddType(s44, "Curry",       "curry");
        seed.AddType(s44, "Pasta",       "pasta");
        seed.AddType(s44, "Soup",        "soup");
        var s45 = seed.AddSubcategory(c7, "Canned meals", "canned-meals");
        seed.AddType(s45, "Sardines",    "sardines");
        seed.AddType(s45, "Tuna",        "tuna");
        seed.AddType(s45, "Bean",        "bean");
        seed.AddType(s45, "Stew",        "stew");
        var s46 = seed.AddSubcategory(c7, "Instant meals & kits", "instant-meals");
        seed.AddType(s46, "Cup noodles", "cup-noodles");
        seed.AddType(s46, "Packet noodles","packet-noodles");
        seed.AddType(s46, "Meal kits",   "meal-kits");
        var s47 = seed.AddSubcategory(c7, "Sandwiches, salads & soups", "sandwiches-salads");
        seed.AddType(s47, "Sandwiches",  "sandwiches");
        seed.AddType(s47, "Wraps",       "wraps");
        seed.AddType(s47, "Salads",      "salads");
        seed.AddType(s47, "Soups",       "soups");

        // 1.8 Snacks & Confectionery
        var c8 = seed.AddCategory(d1, "Snacks & Confectionery", "snacks-confectionery", "fa-cookie-bite", 8);
        var s48 = seed.AddSubcategory(c8, "Chips & Crisps", "chips");
        seed.AddType(s48, "Potato",      "potato");
        seed.AddType(s48, "Banana",      "banana");
        seed.AddType(s48, "Tapioca",     "tapioca");
        seed.AddType(s48, "Vegetable",   "vegetable");
        var s49 = seed.AddSubcategory(c8, "Biscuits & Cookies", "biscuits");
        seed.AddType(s49, "Plain",       "plain");
        seed.AddType(s49, "Filled",      "filled");
        seed.AddType(s49, "Chocolate chip","chocolate-chip");
        seed.AddType(s49, "Sandwich",    "sandwich");
        var s50 = seed.AddSubcategory(c8, "Chocolate", "chocolate");
        seed.AddType(s50, "Bars",        "bars");
        seed.AddType(s50, "Boxes",       "boxes");
        seed.AddType(s50, "Truffles",    "truffles");
        seed.AddType(s50, "Date-filled","date-filled");
        var s51 = seed.AddSubcategory(c8, "Candy, Gummies & Marshmallows", "candy");
        seed.AddType(s51, "Gummies",     "gummies");
        seed.AddType(s51, "Hard candy",  "hard-candy");
        seed.AddType(s51, "Lollipops",   "lollipops");
        seed.AddType(s51, "Marshmallows","marshmallows");
        var s52 = seed.AddSubcategory(c8, "Nuts, seeds & popcorn", "nuts-seeds");
        seed.AddType(s52, "Almonds",     "almonds");
        seed.AddType(s52, "Cashews",     "cashews");
        seed.AddType(s52, "Pistachios",  "pistachios");
        seed.AddType(s52, "Mixed nuts",  "mixed-nuts");
        seed.AddType(s52, "Seeds",       "seeds");
        seed.AddType(s52, "Popcorn",     "popcorn");
        var s53 = seed.AddSubcategory(c8, "Snack & protein bars", "snack-bars");
        seed.AddType(s53, "Granola",     "granola");
        seed.AddType(s53, "Protein",     "protein");
        seed.AddType(s53, "Energy",      "energy");
        seed.AddType(s53, "Fruit",       "fruit");

        // 1.9 Bakery
        var c9 = seed.AddCategory(d1, "Bakery", "bakery", "fa-bread-slice", 9);
        var s54 = seed.AddSubcategory(c9, "Bread", "bread");
        seed.AddType(s54, "Loaf",        "loaf");
        seed.AddType(s54, "Sliced",      "sliced");
        seed.AddType(s54, "Wholemeal",   "wholemeal");
        seed.AddType(s54, "Sourdough",   "sourdough");
        seed.AddType(s54, "Buns & rolls","buns-rolls");
        var s55 = seed.AddSubcategory(c9, "Cakes & Pastries", "cakes");
        seed.AddType(s55, "Layer cake",  "layer-cake");
        seed.AddType(s55, "Cupcakes",    "cupcakes");
        seed.AddType(s55, "Pastries",    "pastries");
        seed.AddType(s55, "Croissants",  "croissants");
        seed.AddType(s55, "Donuts",      "donuts");
        seed.AddType(s55, "Pies & tarts","pies-tarts");
        var s56 = seed.AddSubcategory(c9, "Traditional desserts", "traditional-desserts");
        seed.AddType(s56, "Baklava",     "baklava");
        seed.AddType(s56, "Kueh",        "kueh");
        seed.AddType(s56, "Halva",       "halva");
        seed.AddType(s56, "Ma'amoul",    "maamoul");

        // 1.10 Sauces, Spices & Condiments
        var c10 = seed.AddCategory(d1, "Sauces, Spices & Condiments", "sauces-spices", "fa-pepper-hot", 10);
        var s57 = seed.AddSubcategory(c10, "Sauces & Pastes", "sauces");
        seed.AddType(s57, "Chili sauce", "chili-sauce");
        seed.AddType(s57, "Soy sauce",   "soy-sauce");
        seed.AddType(s57, "Marinade",    "marinade");
        seed.AddType(s57, "Tomato sauce","tomato-sauce");
        seed.AddType(s57, "Sambal",      "sambal");
        var s58 = seed.AddSubcategory(c10, "Spice mixes & single spices", "spices");
        seed.AddType(s58, "Curry powder","curry-powder");
        seed.AddType(s58, "Turmeric",    "turmeric");
        seed.AddType(s58, "Cumin",       "cumin");
        seed.AddType(s58, "Cinnamon",    "cinnamon");
        seed.AddType(s58, "Black pepper","black-pepper");
        seed.AddType(s58, "Cardamom",    "cardamom");
        seed.AddType(s58, "Saffron",     "saffron");
        seed.AddType(s58, "Za'atar",     "zaatar");
        var s59 = seed.AddSubcategory(c10, "Stock & bouillon", "stock");
        seed.AddType(s59, "Chicken stock","chicken-stock");
        seed.AddType(s59, "Beef stock",  "beef-stock");
        seed.AddType(s59, "Vegetable stock","vegetable-stock");
        seed.AddType(s59, "Bouillon cubes","bouillon-cubes");

        // 1.11 Cooking Oils & Fats
        var c11 = seed.AddCategory(d1, "Cooking Oils & Fats", "oils-fats", "fa-bottle-droplet", 11);
        var s60 = seed.AddSubcategory(c11, "Cooking oils", "cooking-oils");
        seed.AddType(s60, "Olive oil",   "olive-oil");
        seed.AddType(s60, "Sunflower oil","sunflower-oil");
        seed.AddType(s60, "Canola oil",  "canola-oil");
        seed.AddType(s60, "Coconut oil", "coconut-oil");
        seed.AddType(s60, "Sesame oil",  "sesame-oil");
        seed.AddType(s60, "Palm oil",    "palm-oil");
        seed.AddType(s60, "Avocado oil", "avocado-oil");
        var s61 = seed.AddSubcategory(c11, "Ghee & rendered fats", "ghee");
        seed.AddType(s61, "Cow ghee",    "cow-ghee");
        seed.AddType(s61, "Buffalo ghee","buffalo-ghee");
        seed.AddType(s61, "Goat ghee",   "goat-ghee");
        var s62 = seed.AddSubcategory(c11, "Shortening & specialty", "shortening");
        seed.AddType(s62, "Vegetable shortening","vegetable-shortening");
        seed.AddType(s62, "Butter ghee blend","butter-ghee-blend");

        // 1.12 Beverages
        var c12 = seed.AddCategory(d1, "Beverages", "beverages", "fa-mug-saucer", 12);
        var s63 = seed.AddSubcategory(c12, "Bottled water & hydration", "water");
        seed.AddType(s63, "Still",       "still");
        seed.AddType(s63, "Sparkling",   "sparkling");
        seed.AddType(s63, "Mineral",     "mineral");
        seed.AddType(s63, "Alkaline",    "alkaline");
        seed.AddType(s63, "Coconut water","coconut-water");
        var s64 = seed.AddSubcategory(c12, "Juice & juice drinks", "juice");
        seed.AddType(s64, "Orange",      "orange");
        seed.AddType(s64, "Apple",       "apple");
        seed.AddType(s64, "Mango",       "mango");
        seed.AddType(s64, "Mixed",       "mixed");
        seed.AddType(s64, "Cold-pressed","cold-pressed");
        var s65 = seed.AddSubcategory(c12, "Soft drinks & mixers", "soft-drinks");
        seed.AddType(s65, "Cola",        "cola");
        seed.AddType(s65, "Lemonade",    "lemonade");
        seed.AddType(s65, "Tonic",       "tonic");
        seed.AddType(s65, "Soda",        "soda");
        var s66 = seed.AddSubcategory(c12, "Tea", "tea");
        seed.AddType(s66, "Black",       "black");
        seed.AddType(s66, "Green",       "green");
        seed.AddType(s66, "Herbal",      "herbal");
        seed.AddType(s66, "Chai",        "chai");
        seed.AddType(s66, "Loose-leaf",  "loose-leaf");
        seed.AddType(s66, "Bags",        "bags");
        seed.AddType(s66, "Instant",     "instant");
        var s67 = seed.AddSubcategory(c12, "Coffee", "coffee");
        seed.AddType(s67, "Beans",       "beans");
        seed.AddType(s67, "Ground",      "ground");
        seed.AddType(s67, "Instant",     "instant");
        seed.AddType(s67, "Capsules",    "capsules");
        seed.AddType(s67, "Arabic",      "arabic");
        seed.AddType(s67, "Decaf",       "decaf");
        var s68 = seed.AddSubcategory(c12, "Functional, sports & energy", "functional");
        seed.AddType(s68, "Sports drink","sports-drink");
        seed.AddType(s68, "Energy drink","energy-drink");
        seed.AddType(s68, "Probiotic",   "probiotic");
        seed.AddType(s68, "Collagen-alternative","collagen-alt");
        var s69 = seed.AddSubcategory(c12, "Plant-based drinks", "plant-drinks");
        seed.AddType(s69, "Oat milk",    "oat-milk");
        seed.AddType(s69, "Almond milk", "almond-milk");
        seed.AddType(s69, "Soy milk",    "soy-milk");

        // 1.13 Baby & Children's Food
        var c13 = seed.AddCategory(d1, "Baby & Children's Food", "baby-food", "fa-baby", 13);
        var s70 = seed.AddSubcategory(c13, "Baby food", "baby-food");
        seed.AddType(s70, "Puree",       "puree");
        seed.AddType(s70, "Cereal",      "cereal");
        seed.AddType(s70, "Snacks",      "snacks");
        var s71 = seed.AddSubcategory(c13, "Infant formula", "infant-formula");
        seed.AddType(s71, "Stage 1",     "stage-1");
        seed.AddType(s71, "Stage 2",     "stage-2");
        seed.AddType(s71, "Stage 3",     "stage-3");
        seed.AddType(s71, "Toddler",     "toddler");
        seed.AddType(s71, "Goat milk",   "goat-milk");
        var s72 = seed.AddSubcategory(c13, "Children's drinks", "children-drinks");
        seed.AddType(s72, "Juice",       "juice");
        seed.AddType(s72, "Milk",        "milk");
        seed.AddType(s72, "Water",       "water");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 2 — Personal Care & Beauty                     ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d2 = seed.AddDepartment("Personal Care & Beauty", "personal-care", "fa-spray-can-sparkles", "Halal-certified skincare, haircare, fragrance, bath & body, makeup.", 2);

        var c14 = seed.AddCategory(d2, "Skincare", "skincare", "fa-face-smile", 1);
        var s73 = seed.AddSubcategory(c14, "Facial care", "facial");
        seed.AddType(s73, "Cleansers",   "cleansers");
        seed.AddType(s73, "Toners",      "toners");
        seed.AddType(s73, "Serums",      "serums");
        seed.AddType(s73, "Moisturizers","moisturizers");
        seed.AddType(s73, "Face masks",  "face-masks");
        seed.AddType(s73, "Sunscreen",   "sunscreen");
        seed.AddType(s73, "Eye care",    "eye-care");
        seed.AddType(s73, "Lip care",    "lip-care");
        var s74 = seed.AddSubcategory(c14, "Body care", "body-care");
        seed.AddType(s74, "Lotions",     "lotions");
        seed.AddType(s74, "Hand creams", "hand-creams");
        seed.AddType(s74, "Foot care",   "foot-care");
        var s75 = seed.AddSubcategory(c14, "Targeted treatments", "treatments");
        seed.AddType(s75, "Acne care",   "acne-care");
        seed.AddType(s75, "Anti-aging",  "anti-aging");
        seed.AddType(s75, "Brightening", "brightening");

        var c15 = seed.AddCategory(d2, "Haircare", "haircare", "fa-spray-can", 2);
        var s76 = seed.AddSubcategory(c15, "Shampoo & Conditioner", "shampoo-conditioner");
        seed.AddType(s76, "Shampoo",     "shampoo");
        seed.AddType(s76, "Conditioner", "conditioner");
        seed.AddType(s76, "2-in-1",      "2-in-1");
        seed.AddType(s76, "Dry shampoo", "dry-shampoo");
        var s77 = seed.AddSubcategory(c15, "Treatments & styling", "treatments-styling");
        seed.AddType(s77, "Hair masks",  "hair-masks");
        seed.AddType(s77, "Hair oils",   "hair-oils");
        seed.AddType(s77, "Styling",     "styling");
        seed.AddType(s77, "Hair colour", "hair-colour");
        seed.AddType(s77, "Scalp care",  "scalp-care");

        var c16 = seed.AddCategory(d2, "Cosmetics & Makeup", "cosmetics-makeup", "fa-paintbrush", 3);
        var s78 = seed.AddSubcategory(c16, "Face", "face");
        seed.AddType(s78, "Foundation",  "foundation");
        seed.AddType(s78, "Concealer",   "concealer");
        seed.AddType(s78, "Powder",      "powder");
        seed.AddType(s78, "Blush",       "blush");
        seed.AddType(s78, "Bronzer",     "bronzer");
        seed.AddType(s78, "Highlighter", "highlighter");
        var s79 = seed.AddSubcategory(c16, "Eyes", "eyes");
        seed.AddType(s79, "Eyeliner",    "eyeliner");
        seed.AddType(s79, "Mascara",     "mascara");
        seed.AddType(s79, "Eyeshadow",   "eyeshadow");
        var s80 = seed.AddSubcategory(c16, "Lips", "lips");
        seed.AddType(s80, "Lipstick",    "lipstick");
        seed.AddType(s80, "Lip gloss",   "lip-gloss");
        seed.AddType(s80, "Lip balm",    "lip-balm");
        var s81 = seed.AddSubcategory(c16, "Makeup tools & removers", "tools");
        seed.AddType(s81, "Removers",    "removers");
        seed.AddType(s81, "Brushes",     "brushes");
        seed.AddType(s81, "Sponges",     "sponges");

        var c17 = seed.AddCategory(d2, "Fragrance", "fragrance", "fa-wind", 4);
        var s82 = seed.AddSubcategory(c17, "Perfume", "perfume");
        seed.AddType(s82, "Eau de parfum","edp");
        seed.AddType(s82, "Eau de toilette","edt");
        seed.AddType(s82, "Body mist",   "body-mist");
        seed.AddType(s82, "Roll-on",     "roll-on");
        var s83 = seed.AddSubcategory(c17, "Oud, Attar & Bakhoor", "oud-attar");
        seed.AddType(s83, "Oud",         "oud");
        seed.AddType(s83, "Attar",       "attar");
        seed.AddType(s83, "Bakhoor",     "bakhoor");
        seed.AddType(s83, "Incense",     "incense");
        seed.AddType(s83, "Perfume oils","perfume-oils");

        var c18 = seed.AddCategory(d2, "Bath & Body", "bath-body", "fa-shower", 5);
        var s84 = seed.AddSubcategory(c18, "Cleansing", "cleansing");
        seed.AddType(s84, "Body wash",   "body-wash");
        seed.AddType(s84, "Hand wash",   "hand-wash");
        seed.AddType(s84, "Bar soap",    "bar-soap");
        seed.AddType(s84, "Shower gel",  "shower-gel");
        var s85 = seed.AddSubcategory(c18, "Spa & treatments", "spa");
        seed.AddType(s85, "Scrubs",      "scrubs");
        seed.AddType(s85, "Bath salts",  "bath-salts");
        seed.AddType(s85, "Body oils",   "body-oils");
        seed.AddType(s85, "Deodorant",   "deodorant");
        var s86 = seed.AddSubcategory(c18, "Feminine hygiene", "feminine");
        seed.AddType(s86, "Wipes",       "wipes");
        seed.AddType(s86, "Wash",        "wash");
        seed.AddType(s86, "Pads",        "pads");

        var c19 = seed.AddCategory(d2, "Oral Care", "oral-care", "fa-tooth", 6);
        var s87 = seed.AddSubcategory(c19, "Toothpaste & mouthwash", "toothpaste");
        seed.AddType(s87, "Toothpaste",  "toothpaste");
        seed.AddType(s87, "Mouthwash",   "mouthwash");
        seed.AddType(s87, "Miswak",      "miswak");
        var s88 = seed.AddSubcategory(c19, "Tools", "tools");
        seed.AddType(s88, "Toothbrushes","toothbrushes");
        seed.AddType(s88, "Dental floss","floss");
        seed.AddType(s88, "Interdental", "interdental");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 3 — Health, Wellness & Nutrition              ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d3 = seed.AddDepartment("Health, Wellness & Nutrition", "health-wellness", "fa-heart-pulse", "Halal-verified vitamins, supplements, herbal products, healthcare, and specialty nutrition.", 3);

        var c20 = seed.AddCategory(d3, "Vitamins & Supplements", "vitamins", "fa-pills", 1);
        var s89 = seed.AddSubcategory(c20, "By type", "by-type");
        seed.AddType(s89, "Multivitamins","multivitamins");
        seed.AddType(s89, "Vitamin C",   "vitamin-c");
        seed.AddType(s89, "Vitamin D",   "vitamin-d");
        seed.AddType(s89, "B-complex",   "b-complex");
        seed.AddType(s89, "Minerals",    "minerals");
        seed.AddType(s89, "Iron",        "iron");
        seed.AddType(s89, "Calcium",     "calcium");
        seed.AddType(s89, "Zinc",        "zinc");
        seed.AddType(s89, "Magnesium",   "magnesium");
        var s90 = seed.AddSubcategory(c20, "Specialty", "specialty");
        seed.AddType(s90, "Protein",     "protein");
        seed.AddType(s90, "Amino acids", "amino-acids");
        seed.AddType(s90, "Omega oils",  "omega-oils");
        seed.AddType(s90, "Probiotics",  "probiotics");
        seed.AddType(s90, "Collagen alternatives","collagen-alt");
        seed.AddType(s90, "Children's",  "children");

        var c21 = seed.AddCategory(d3, "Herbal & Traditional", "herbal-traditional", "fa-mortar-pestle", 2);
        var s91 = seed.AddSubcategory(c21, "Herbal supplements", "herbal-supps");
        seed.AddType(s91, "Habbatus sauda","habbatus-sauda");
        seed.AddType(s91, "Tongkat ali", "tongkat-ali");
        seed.AddType(s91, "Turmeric",    "turmeric");
        seed.AddType(s91, "Ginger",      "ginger");
        seed.AddType(s91, "Ashwagandha", "ashwagandha");
        var s92 = seed.AddSubcategory(c21, "Traditional remedies", "traditional");
        seed.AddType(s92, "Honey",       "honey");
        seed.AddType(s92, "Tualang honey","tualang-honey");
        seed.AddType(s92, "Manuka honey","manuka-honey");
        seed.AddType(s92, "Royal jelly", "royal-jelly");
        seed.AddType(s92, "Propolis",    "propolis");
        var s93 = seed.AddSubcategory(c21, "Essential oils & extracts", "essential-oils");
        seed.AddType(s93, "Lavender",    "lavender");
        seed.AddType(s93, "Tea tree",    "tea-tree");
        seed.AddType(s93, "Eucalyptus",  "eucalyptus");
        seed.AddType(s93, "Frankincense","frankincense");
        var s94 = seed.AddSubcategory(c21, "Herbal teas", "herbal-teas");
        seed.AddType(s94, "Chamomile",   "chamomile");
        seed.AddType(s94, "Peppermint",  "peppermint");
        seed.AddType(s94, "Ginger tea",  "ginger-tea");
        seed.AddType(s94, "Tulsi",       "tulsi");

        var c22 = seed.AddCategory(d3, "Healthcare & First Aid", "healthcare", "fa-briefcase-medical", 3);
        var s95 = seed.AddSubcategory(c22, "First aid", "first-aid");
        seed.AddType(s95, "Bandages",    "bandages");
        seed.AddType(s95, "Antiseptics","antiseptics");
        seed.AddType(s95, "Pain relief","pain-relief");
        seed.AddType(s95, "Cough & cold","cough-cold");
        var s96 = seed.AddSubcategory(c22, "Medical devices", "medical-devices");
        seed.AddType(s96, "Thermometers","thermometers");
        seed.AddType(s96, "Blood pressure","blood-pressure");
        seed.AddType(s96, "Glucose monitors","glucose-monitors");
        seed.AddType(s96, "Mobility aids","mobility-aids");

        var c23 = seed.AddCategory(d3, "Specialty Nutrition", "specialty-nutrition", "fa-wheat-awn-circle-check", 4);
        var s97 = seed.AddSubcategory(c23, "Sports nutrition", "sports");
        seed.AddType(s97, "Whey",        "whey");
        seed.AddType(s97, "Plant protein","plant-protein");
        seed.AddType(s97, "Mass gainer", "mass-gainer");
        seed.AddType(s97, "BCAAs",       "bcaas");
        seed.AddType(s97, "Pre-workout", "pre-workout");
        var s98 = seed.AddSubcategory(c23, "Meal replacement", "meal-replacement");
        seed.AddType(s98, "Shakes",      "shakes");
        seed.AddType(s98, "Bars",        "bars");
        var s99 = seed.AddSubcategory(c23, "Special dietary foods", "special-dietary");
        seed.AddType(s99, "Diabetic-friendly","diabetic-friendly");
        seed.AddType(s99, "Weight-management","weight-management");
        seed.AddType(s99, "Renal-friendly","renal-friendly");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 4 — Household & Home                           ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d4 = seed.AddDepartment("Household & Home", "household", "fa-house", "Halal-conscious home care, kitchen, and home fragrance.", 4);

        var c24 = seed.AddCategory(d4, "Household Cleaning", "cleaning", "fa-spray-can-sparkles", 1);
        var s100 = seed.AddSubcategory(c24, "Surface cleaners", "surface");
        seed.AddType(s100, "All-purpose", "all-purpose");
        seed.AddType(s100, "Kitchen",    "kitchen");
        seed.AddType(s100, "Bathroom",   "bathroom");
        seed.AddType(s100, "Floor",      "floor");
        seed.AddType(s100, "Glass",      "glass");
        seed.AddType(s100, "Disinfectant","disinfectant");
        var s101 = seed.AddSubcategory(c24, "Laundry", "laundry");
        seed.AddType(s101, "Detergent",  "detergent");
        seed.AddType(s101, "Fabric softener","softener");
        seed.AddType(s101, "Stain remover","stain-remover");
        var s102 = seed.AddSubcategory(c24, "Dishwashing", "dishwashing");
        seed.AddType(s102, "Dish soap",  "dish-soap");
        seed.AddType(s102, "Dishwasher tablets","dishwasher-tablets");
        seed.AddType(s102, "Sponges",    "sponges");

        var c25 = seed.AddCategory(d4, "Home Fragrance", "home-fragrance", "fa-spray-can", 2);
        var s103 = seed.AddSubcategory(c25, "Active fragrance", "active");
        seed.AddType(s103, "Air freshener spray","air-spray");
        seed.AddType(s103, "Reed diffusers","reed-diffusers");
        seed.AddType(s103, "Scented candles","candles");
        seed.AddType(s103, "Electric diffusers","electric");
        var s104 = seed.AddSubcategory(c25, "Traditional fragrance", "traditional-fragrance");
        seed.AddType(s104, "Bakhoor",    "bakhoor");
        seed.AddType(s104, "Oud chips",  "oud-chips");
        seed.AddType(s104, "Incense",    "incense");
        seed.AddType(s104, "Mabkharat",  "mabkharat");

        var c26 = seed.AddCategory(d4, "Kitchen", "kitchen", "fa-kitchen-set", 3);
        var s105 = seed.AddSubcategory(c26, "Cookware", "cookware");
        seed.AddType(s105, "Pots",       "pots");
        seed.AddType(s105, "Pans",       "pans");
        seed.AddType(s105, "Woks",       "woks");
        seed.AddType(s105, "Pressure cookers","pressure-cookers");
        seed.AddType(s105, "Rice cookers","rice-cookers");
        var s106 = seed.AddSubcategory(c26, "Bakeware", "bakeware");
        seed.AddType(s106, "Baking trays","trays");
        seed.AddType(s106, "Cake tins",  "cake-tins");
        seed.AddType(s106, "Moulds",     "moulds");
        var s107 = seed.AddSubcategory(c26, "Utensils & tools", "utensils");
        seed.AddType(s107, "Spatulas",   "spatulas");
        seed.AddType(s107, "Tongs",      "tongs");
        seed.AddType(s107, "Whisks",     "whisks");
        seed.AddType(s107, "Cutting boards","cutting-boards");
        seed.AddType(s107, "Knives",     "knives");
        var s108 = seed.AddSubcategory(c26, "Storage & hydration", "storage");
        seed.AddType(s108, "Food containers","containers");
        seed.AddType(s108, "Water bottles","water-bottles");
        seed.AddType(s108, "Lunch boxes","lunch-boxes");
        seed.AddType(s108, "Thermos",    "thermos");

        var c27 = seed.AddCategory(d4, "Home Essentials", "home-essentials", "fa-bed", 4);
        var s109 = seed.AddSubcategory(c27, "Textiles", "textiles");
        seed.AddType(s109, "Bedding",    "bedding");
        seed.AddType(s109, "Towels",     "towels");
        seed.AddType(s109, "Curtains",   "curtains");
        seed.AddType(s109, "Prayer mats","prayer-mats");
        var s110 = seed.AddSubcategory(c27, "Storage & accessories", "home-accessories");
        seed.AddType(s110, "Storage boxes","storage-boxes");
        seed.AddType(s110, "Organisers", "organisers");
        seed.AddType(s110, "Hooks",      "hooks");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 5 — Modest Fashion & Lifestyle                 ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d5 = seed.AddDepartment("Modest Fashion & Lifestyle", "modest-fashion", "fa-shirt", "Modest clothing, hijab, footwear, and accessories for the whole family.", 5);

        var c28 = seed.AddCategory(d5, "Women's Clothing", "womens-clothing", "fa-person-dress", 1);
        var s111 = seed.AddSubcategory(c28, "Abayas", "abayas");
        seed.AddType(s111, "Closed",     "closed");
        seed.AddType(s111, "Open",       "open");
        seed.AddType(s111, "Embroidered","embroidered");
        seed.AddType(s111, "Everyday",   "everyday");
        seed.AddType(s111, "Occasion",   "occasion");
        seed.AddType(s111, "Kimono-cut", "kimono-cut");
        var s112 = seed.AddSubcategory(c28, "Hijabs & head coverings", "hijabs");
        seed.AddType(s112, "Chiffon",    "chiffon");
        seed.AddType(s112, "Jersey",     "jersey");
        seed.AddType(s112, "Silk",       "silk");
        seed.AddType(s112, "Cotton",     "cotton");
        seed.AddType(s112, "Sports",     "sports");
        seed.AddType(s112, "Instant",    "instant");
        seed.AddType(s112, "Khimar",     "khimar");
        var s113 = seed.AddSubcategory(c28, "Prayer wear", "prayer-wear-women");
        seed.AddType(s113, "Prayer dress","prayer-dress");
        seed.AddType(s113, "Mukena",     "mukena");
        seed.AddType(s113, "Telekung",   "telekung");
        var s114 = seed.AddSubcategory(c28, "Modest everyday", "modest-everyday");
        seed.AddType(s114, "Tops",       "tops");
        seed.AddType(s114, "Skirts",     "skirts");
        seed.AddType(s114, "Trousers",   "trousers");
        seed.AddType(s114, "Dresses",    "dresses");
        seed.AddType(s114, "Modest swimwear","modest-swimwear");

        var c29 = seed.AddCategory(d5, "Men's Clothing", "mens-clothing", "fa-person", 2);
        var s115 = seed.AddSubcategory(c29, "Thobes & jubbas", "thobes");
        seed.AddType(s115, "Saudi",      "saudi");
        seed.AddType(s115, "Emirati",    "emirati");
        seed.AddType(s115, "Malaysian",  "malaysian");
        seed.AddType(s115, "Plain",      "plain");
        seed.AddType(s115, "Embroidered","embroidered");
        var s116 = seed.AddSubcategory(c29, "Kurta & Baju", "kurta-baju");
        seed.AddType(s116, "Kurta",      "kurta");
        seed.AddType(s116, "Baju Melayu","baju-melayu");
        seed.AddType(s116, "Baju Koko",  "baju-koko");
        var s117 = seed.AddSubcategory(c29, "Shirts & trousers", "shirts-trousers");
        seed.AddType(s117, "Modest shirts","modest-shirts");
        seed.AddType(s117, "Modest trousers","modest-trousers");
        var s118 = seed.AddSubcategory(c29, "Prayer wear", "prayer-wear-men");
        seed.AddType(s118, "Sarong",     "sarong");
        seed.AddType(s118, "Kufi",       "kufi");
        seed.AddType(s118, "Topi",       "topi");

        var c30 = seed.AddCategory(d5, "Children's Clothing", "kids-clothing", "fa-children", 3);
        var s119 = seed.AddSubcategory(c30, "Girls", "girls");
        seed.AddType(s119, "Hijabs",     "hijabs");
        seed.AddType(s119, "Abayas",     "abayas");
        seed.AddType(s119, "Dresses",    "dresses");
        seed.AddType(s119, "Tops",       "tops");
        var s120 = seed.AddSubcategory(c30, "Boys", "boys");
        seed.AddType(s120, "Thobes",     "thobes");
        seed.AddType(s120, "Baju Melayu","baju-melayu");
        seed.AddType(s120, "Kufi",       "kufi");
        seed.AddType(s120, "T-shirts",   "t-shirts");
        var s121 = seed.AddSubcategory(c30, "Prayer wear", "kids-prayer");
        seed.AddType(s121, "Sarong",     "sarong");
        seed.AddType(s121, "Mukena",     "mukena");
        seed.AddType(s121, "Kufi",       "kufi");

        var c31 = seed.AddCategory(d5, "Accessories", "fashion-accessories", "fa-bag-shopping", 4);
        var s122 = seed.AddSubcategory(c31, "Hijab accessories", "hijab-accessories");
        seed.AddType(s122, "Pins",       "pins");
        seed.AddType(s122, "Brooches",   "brooches");
        seed.AddType(s122, "Magnets",    "magnets");
        seed.AddType(s122, "Undercaps",  "undercaps");
        seed.AddType(s122, "Bands",      "bands");
        var s123 = seed.AddSubcategory(c31, "Bags & wallets", "bags-wallets");
        seed.AddType(s123, "Handbags",   "handbags");
        seed.AddType(s123, "Crossbody",  "crossbody");
        seed.AddType(s123, "Tote",       "tote");
        seed.AddType(s123, "Backpack",   "backpack");
        seed.AddType(s123, "Wallets",    "wallets");
        var s124 = seed.AddSubcategory(c31, "Belts & small accessories", "belts");
        seed.AddType(s124, "Belts",      "belts");
        seed.AddType(s124, "Sunglasses", "sunglasses");
        seed.AddType(s124, "Watches",    "watches");

        var c32 = seed.AddCategory(d5, "Footwear", "footwear", "fa-shoe-prints", 5);
        var s125 = seed.AddSubcategory(c32, "By type", "by-type");
        seed.AddType(s125, "Sandals",    "sandals");
        seed.AddType(s125, "Shoes",      "shoes");
        seed.AddType(s125, "Slippers",   "slippers");
        seed.AddType(s125, "Boots",      "boots");
        seed.AddType(s125, "Prayer-friendly","prayer-friendly");
        var s126 = seed.AddSubcategory(c32, "By audience", "by-audience");
        seed.AddType(s126, "Women",      "women");
        seed.AddType(s126, "Men",        "men");
        seed.AddType(s126, "Children",   "children");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 6 — Islamic & Religious Essentials              ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d6 = seed.AddDepartment("Islamic & Religious Essentials", "islamic-religious", "fa-mosque", "Qurans, prayer, Islamic books, and religious gifts.", 6);

        var c33 = seed.AddCategory(d6, "Qurans & Quran Accessories", "quran", "fa-book-quran", 1);
        var s127 = seed.AddSubcategory(c33, "Qurans", "qurans");
        seed.AddType(s127, "Arabic",     "arabic");
        seed.AddType(s127, "Translation","translation");
        seed.AddType(s127, "Tafsir",     "tafsir");
        seed.AddType(s127, "Pocket size","pocket");
        seed.AddType(s127, "Large print","large-print");
        var s128 = seed.AddSubcategory(c33, "Quran accessories", "quran-accessories");
        seed.AddType(s128, "Quran stand","quran-stand");
        seed.AddType(s128, "Quran cover","quran-cover");
        seed.AddType(s128, "Digital Quran","digital-quran");
        seed.AddType(s128, "Quran holder","quran-holder");

        var c34 = seed.AddCategory(d6, "Prayer", "prayer", "fa-person-praying", 2);
        var s129 = seed.AddSubcategory(c34, "Prayer mats", "prayer-mats");
        seed.AddType(s129, "Velvet",     "velvet");
        seed.AddType(s129, "Foam",       "foam");
        seed.AddType(s129, "Travel",     "travel");
        seed.AddType(s129, "Children's","children");
        var s130 = seed.AddSubcategory(c34, "Prayer accessories", "prayer-accessories");
        seed.AddType(s130, "Tasbih",     "tasbih");
        seed.AddType(s130, "Digital tasbih","digital-tasbih");
        seed.AddType(s130, "Qibla compass","qibla-compass");
        seed.AddType(s130, "Prayer cap", "prayer-cap");
        seed.AddType(s130, "Topi",       "topi");

        var c35 = seed.AddCategory(d6, "Islamic Books & Education", "islamic-books", "fa-book", 3);
        var s131 = seed.AddSubcategory(c35, "Hadith & Fiqh", "hadith-fiqh");
        seed.AddType(s131, "Hadith",     "hadith");
        seed.AddType(s131, "Fiqh",       "fiqh");
        seed.AddType(s131, "Aqeedah",    "aqeedah");
        var s132 = seed.AddSubcategory(c35, "Children's Islamic", "children-islamic");
        seed.AddType(s132, "Story books","story-books");
        seed.AddType(s132, "Quran stories","quran-stories");
        seed.AddType(s132, "Activity books","activity-books");
        var s133 = seed.AddSubcategory(c35, "Islamic history & literature", "history");
        seed.AddType(s133, "Seerah",     "seerah");
        seed.AddType(s133, "History",    "history");
        seed.AddType(s133, "Biography",  "biography");

        var c36 = seed.AddCategory(d6, "Islamic Gifts & Decor", "islamic-gifts", "fa-gift", 4);
        var s134 = seed.AddSubcategory(c36, "Wall art & calligraphy", "wall-art");
        seed.AddType(s134, "Canvas",     "canvas");
        seed.AddType(s134, "Metal",      "metal");
        seed.AddType(s134, "Wood",       "wood");
        var s135 = seed.AddSubcategory(c36, "Ornaments & gifts", "ornaments");
        seed.AddType(s135, "Ornaments",  "ornaments");
        seed.AddType(s135, "Gift sets",  "gift-sets");
        seed.AddType(s135, "Ramadan gifts","ramadan-gifts");
        seed.AddType(s135, "Eid gifts",  "eid-gifts");
        seed.AddType(s135, "Hajj & Umrah gifts","hajj-umrah-gifts");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 7 — Baby, Mother & Maternity                    ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d7 = seed.AddDepartment("Baby, Mother & Maternity", "baby-mother", "fa-baby-carriage", "Halal-conscious baby care, mother & maternity products, and feeding essentials.", 7);

        var c37 = seed.AddCategory(d7, "Baby Food", "baby-food-d7", "fa-jar", 1);
        var s136 = seed.AddSubcategory(c37, "Formula & milk", "formula-milk");
        seed.AddType(s136, "Infant formula","infant-formula");
        seed.AddType(s136, "Follow-on",  "follow-on");
        seed.AddType(s136, "Toddler milk","toddler-milk");
        seed.AddType(s136, "Goat milk",  "goat-milk");
        seed.AddType(s136, "Plant-based","plant-based");
        var s137 = seed.AddSubcategory(c37, "Cereals & meals", "cereals-meals");
        seed.AddType(s137, "Cereal",     "cereal");
        seed.AddType(s137, "Puree pouches","puree-pouches");
        seed.AddType(s137, "Snacks",     "snacks");
        var s138 = seed.AddSubcategory(c37, "Baby drinks", "baby-drinks");
        seed.AddType(s138, "Water",      "water");
        seed.AddType(s138, "Juice",      "juice");

        var c38 = seed.AddCategory(d7, "Baby Care", "baby-care", "fa-baby", 2);
        var s139 = seed.AddSubcategory(c38, "Bath & skin", "bath-skin");
        seed.AddType(s139, "Shampoo",    "shampoo");
        seed.AddType(s139, "Body wash",  "body-wash");
        seed.AddType(s139, "Lotion",     "lotion");
        seed.AddType(s139, "Oil",        "oil");
        seed.AddType(s139, "Powder",     "powder");
        var s140 = seed.AddSubcategory(c38, "Diapers & wipes", "diapers-wipes");
        seed.AddType(s140, "Disposable diapers","disposable-diapers");
        seed.AddType(s140, "Cloth diapers","cloth-diapers");
        seed.AddType(s140, "Wipes",      "wipes");
        seed.AddType(s140, "Nappies",    "nappies");

        var c39 = seed.AddCategory(d7, "Mother & Maternity", "maternity", "fa-person-pregnant", 3);
        var s141 = seed.AddSubcategory(c39, "Maternity clothing", "maternity-clothing");
        seed.AddType(s141, "Dresses",    "dresses");
        seed.AddType(s141, "Tops",       "tops");
        seed.AddType(s141, "Trousers",   "trousers");
        seed.AddType(s141, "Hijabs",     "hijabs");
        var s142 = seed.AddSubcategory(c39, "Nursing & breastfeeding", "nursing");
        seed.AddType(s142, "Nursing wear","nursing-wear");
        seed.AddType(s142, "Covers",     "covers");
        seed.AddType(s142, "Pumps",      "pumps");
        seed.AddType(s142, "Pads",       "pads");
        var s143 = seed.AddSubcategory(c39, "Postpartum care", "postpartum");
        seed.AddType(s143, "Belly binding","belly-binding");
        seed.AddType(s143, "Herbal",     "herbal");
        seed.AddType(s143, "Skincare",   "skincare");

        var c40 = seed.AddCategory(d7, "Baby Essentials", "baby-essentials", "fa-baby", 4);
        var s144 = seed.AddSubcategory(c40, "Feeding", "feeding");
        seed.AddType(s144, "Bottles",    "bottles");
        seed.AddType(s144, "Pacifiers",  "pacifiers");
        seed.AddType(s144, "Utensils",   "utensils");
        seed.AddType(s144, "Sterilisers","sterilisers");
        var s145 = seed.AddSubcategory(c40, "Sleep & comfort", "sleep-comfort");
        seed.AddType(s145, "Bedding",    "bedding");
        seed.AddType(s145, "Swaddles",   "swaddles");
        seed.AddType(s145, "Sleep sacks","sleep-sacks");
        var s146 = seed.AddSubcategory(c40, "Accessories", "baby-accessories");
        seed.AddType(s146, "Pacifier clips","pacifier-clips");
        seed.AddType(s146, "Teethers",   "teethers");
        seed.AddType(s146, "Wipes warmers","wipes-warmers");

        // ╔═══════════════════════════════════════════════════════════╗
        // ║  Department 8 — Business, B2B & Wholesale (gated)          ║
        // ╚═══════════════════════════════════════════════════════════╝
        var d8 = seed.AddDepartment("Business, B2B & Wholesale", "b2b", "fa-building", "Bulk halal ingredients, OEM, private label, and HoReCa supplies. Visible to verified business accounts.", 8);

        var c41 = seed.AddCategory(d8, "Halal Ingredients", "halal-ingredients", "fa-flask", 1);
        var s147 = seed.AddSubcategory(c41, "Meat ingredients", "meat-ingredients");
        seed.AddType(s147, "Frozen bulk meat","frozen-bulk");
        seed.AddType(s147, "Meat extract","meat-extract");
        seed.AddType(s147, "Meat powder","meat-powder");
        var s148 = seed.AddSubcategory(c41, "Functional ingredients", "functional-ingredients");
        seed.AddType(s148, "Gelatin alternatives","gelatin-alt");
        seed.AddType(s148, "Pectin",     "pectin");
        seed.AddType(s148, "Agar",      "agar");
        seed.AddType(s148, "Emulsifiers","emulsifiers");
        seed.AddType(s148, "Stabilizers","stabilizers");
        seed.AddType(s148, "Thickeners", "thickeners");
        seed.AddType(s148, "Enzymes",    "enzymes");
        var s149 = seed.AddSubcategory(c41, "Flavour & aroma", "flavour");
        seed.AddType(s149, "Natural flavours","natural-flavours");
        seed.AddType(s149, "Artificial flavours","artificial-flavours");
        seed.AddType(s149, "Vanilla",    "vanilla");
        seed.AddType(s149, "Food essences","essences");
        var s150 = seed.AddSubcategory(c41, "Food additives", "additives");
        seed.AddType(s150, "Preservatives","preservatives");
        seed.AddType(s150, "Colourants", "colourants");
        seed.AddType(s150, "Sweeteners", "sweeteners");
        seed.AddType(s150, "Antioxidants","antioxidants");
        var s151 = seed.AddSubcategory(c41, "Processing ingredients", "processing");
        seed.AddType(s151, "Food-grade oils","food-grade-oils");
        seed.AddType(s151, "Cultures",   "cultures");
        seed.AddType(s151, "Fermentation","fermentation");

        var c42 = seed.AddCategory(d8, "Cosmetic Ingredients", "cosmetic-ingredients", "fa-flask-vial", 2);
        var s152 = seed.AddSubcategory(c42, "Oils & butters", "oils-butters");
        seed.AddType(s152, "Coconut oil","coconut-oil");
        seed.AddType(s152, "Argan oil",  "argan-oil");
        seed.AddType(s152, "Jojoba oil", "jojoba-oil");
        seed.AddType(s152, "Shea butter","shea-butter");
        seed.AddType(s152, "Cocoa butter","cocoa-butter");
        var s153 = seed.AddSubcategory(c42, "Waxes", "waxes");
        seed.AddType(s153, "Beeswax",    "beeswax");
        seed.AddType(s153, "Plant wax",  "plant-wax");
        seed.AddType(s153, "Carnauba",   "carnauba");
        var s154 = seed.AddSubcategory(c42, "Extracts & actives", "extracts");
        seed.AddType(s154, "Botanical extracts","botanical");
        seed.AddType(s154, "Fruit extracts","fruit");
        seed.AddType(s154, "Hyaluronic acid","hyaluronic");
        seed.AddType(s154, "Niacinamide","niacinamide");
        var s155 = seed.AddSubcategory(c42, "Fragrance ingredients", "fragrance-ingredients");
        seed.AddType(s155, "Essential oils","essential-oils");
        seed.AddType(s155, "Oud",        "oud");
        seed.AddType(s155, "Natural fragrances","natural-fragrances");

        var c43 = seed.AddCategory(d8, "HoReCa & Food Service", "horeca", "fa-utensils", 3);
        var s156 = seed.AddSubcategory(c43, "Restaurant supplies", "restaurant");
        seed.AddType(s156, "Bulk halal meat","bulk-meat");
        seed.AddType(s156, "Bulk ingredients","bulk-ingredients");
        seed.AddType(s156, "Sauces & pastes","sauces-pastes");
        seed.AddType(s156, "Frozen foods","frozen-foods");
        seed.AddType(s156, "Bakery ingredients","bakery-ingredients");
        var s157 = seed.AddSubcategory(c43, "Catering & packaging", "catering");
        seed.AddType(s157, "Bulk food",  "bulk-food");
        seed.AddType(s157, "Disposable food service","disposables");
        seed.AddType(s157, "Packaging",  "packaging");
        var s158 = seed.AddSubcategory(c43, "Hotel supplies", "hotel");
        seed.AddType(s158, "Halal minibar","minibar");
        seed.AddType(s158, "Muslim-friendly toiletries","toiletries");
        seed.AddType(s158, "Prayer kits","prayer-kits");
        seed.AddType(s158, "Qibla products","qibla-products");

        var c44 = seed.AddCategory(d8, "Private Label & OEM", "oem", "fa-tag", 4);
        var s159 = seed.AddSubcategory(c44, "By product", "by-product");
        seed.AddType(s159, "F&B formulations","fb-formulations");
        seed.AddType(s159, "Cosmetic formulations","cosmetic-formulations");
        seed.AddType(s159, "Supplement formulations","supplement-formulations");
        var s160 = seed.AddSubcategory(c44, "By service", "by-service");
        seed.AddType(s160, "Custom branding","custom-branding");
        seed.AddType(s160, "Contract manufacturing","contract-manufacturing");
        seed.AddType(s160, "Packaging design","packaging-design");

        var c45 = seed.AddCategory(d8, "Wholesale & Bulk", "wholesale", "fa-boxes-stacked", 5);
        var s161 = seed.AddSubcategory(c45, "By category", "by-category");
        seed.AddType(s161, "F&B wholesale","fb-wholesale");
        seed.AddType(s161, "Personal care wholesale","pc-wholesale");
        seed.AddType(s161, "Apparel wholesale","apparel-wholesale");
        var s162 = seed.AddSubcategory(c45, "By logistics", "by-logistics");
        seed.AddType(s162, "Cold chain","cold-chain");
        seed.AddType(s162, "Ambient",    "ambient");
        seed.AddType(s162, "Import/export","import-export");

        var c46 = seed.AddCategory(d8, "Pet & Animal (B2B)", "pet-animal-b2b", "fa-paw", 6);
        var s163 = seed.AddSubcategory(c46, "Pet food (retail packs under dept 1)", "pet-food");
        var s164 = seed.AddSubcategory(c46, "Animal feed (wholesale)", "animal-feed");
        seed.AddType(s164, "Livestock feed","livestock-feed");
        seed.AddType(s164, "Poultry feed","poultry-feed");
        seed.AddType(s164, "Aquaculture feed","aquaculture-feed");
        seed.AddType(s164, "Feed additives","feed-additives");

        // Flush collected nodes into the DbContext
        seed.Commit();
    }
}

/// <summary>
/// Fluent helper that collects taxonomy nodes in memory and flushes them
/// to the ModelBuilder in a single pass at the end (avoids deeply nested
/// method calls in the seed method).
/// </summary>
internal sealed class TaxonomyBuilder
{
    private readonly ModelBuilder _mb;
    private readonly List<Department>    _departments = [];
    private readonly List<TaxonomyCategory> _categories  = [];
    private readonly List<Subcategory>   _subs        = [];
    private readonly List<ProductType>   _types       = [];

    public TaxonomyBuilder(ModelBuilder mb) => _mb = mb;

    public Department AddDepartment(string name, string slug, string icon, string? description, int sortOrder)
    {
        var d = new Department
        {
            Id = DeterministicId(0, sort: sortOrder),
            Name = name, Slug = slug, IconClass = icon, Description = description,
            SortOrder = sortOrder, IsActive = true
        };
        _departments.Add(d);
        return d;
    }

    public TaxonomyCategory AddCategory(Department d, string name, string slug, string? icon, int sortOrder)
    {
        var c = new TaxonomyCategory
        {
            Id = DeterministicId(1, sort: sortOrder, parent: d),
            DepartmentId = d.Id, Name = name, Slug = slug, IconClass = icon,
            SortOrder = sortOrder, IsActive = true
        };
        _categories.Add(c);
        return c;
    }

    public Subcategory AddSubcategory(TaxonomyCategory c, string name, string slug)
    {
        var s = new Subcategory
        {
            Id = DeterministicId(2, sort: _subs.Count + 1, parent: c),
            CategoryId = c.Id, Name = name, Slug = slug, SortOrder = _subs.Count + 1, IsActive = true
        };
        _subs.Add(s);
        return s;
    }

    public ProductType AddType(Subcategory s, string name, string slug)
    {
        // Path slug: dept/cat/sub/type — all kebab-case
        var dept = _departments.First(d => d.Id == _categories.First(c => c.Id == s.CategoryId).DepartmentId);
        var path = $"{dept.Slug}/{_categories.First(c => c.Id == s.CategoryId).Slug}/{s.Slug}/{slug}";
        var t = new ProductType
        {
            Id = DeterministicId(3, sort: _types.Count + 1, parent: s),
            SubcategoryId = s.Id, Name = name, Slug = slug, SortOrder = _types.Count + 1,
            PathSlug = path, IsActive = true
        };
        _types.Add(t);
        return t;
    }

    public void Commit()
    {
        _mb.Entity<Department>().HasData(_departments);
        _mb.Entity<TaxonomyCategory>().HasData(_categories);
        _mb.Entity<Subcategory>().HasData(_subs);
        _mb.Entity<ProductType>().HasData(_types);
    }

    /// <summary>
    /// Build a deterministic GUID from the entity level (0=Dept,1=Cat,2=Sub,3=Type)
    /// and an incrementing sort key. The first byte encodes the level so all
    /// departments share a prefix, all categories another, etc.
    /// </summary>
    private static Guid DeterministicId(int level, int sort, object? parent = null)
    {
        // Use a 4-byte tag = level, then a 12-byte payload from a hash of
        // (parent, sort, name). Deterministic & stable across runs.
        Span<byte> bytes = stackalloc byte[16];
        bytes[0] = (byte)(0xD0 | level);
        // Mix sort + parent id into the rest
        var mix = (uint)sort * 2654435761u;
        if (parent is Department d)  mix = mix ^ (uint)d.Id.GetHashCode();
        if (parent is Category c)    mix = mix ^ (uint)c.Id.GetHashCode();
        if (parent is Subcategory s)  mix = mix ^ (uint)s.Id.GetHashCode();
        for (int i = 1; i < 16; i++)
        {
            mix = mix * 1103515245u + 12345u;
            bytes[i] = (byte)(mix >> 24);
        }
        return new Guid(bytes);
    }
}
