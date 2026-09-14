using HalalChain.Platform.Contracts.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Persistence.Seed;

/// <summary>
/// Fixed-GUID anchor product types. These are stable, well-known
/// ProductType IDs that the legacy product seed (and any out-of-band
/// data import) can reference. They are created as part of the
/// taxonomy seed so they exist before any product references them.
///
/// Why anchors?
///  - The bulk taxonomy seed uses deterministic GUIDs derived from
///    hashes (stable across runs, but not knowable from outside).
///  - External code (legacy seed, migration scripts, Excel imports)
///    needs hard-coded GUIDs to assign products to types.
///  - We solve this by declaring a small set of fixed-GUID anchor
///    types for the product kinds the platform cares about most
///    (e.g. keropok-lekor, teh-tarik, ayam-kampung). New types
///    added later get auto-generated deterministic GUIDs.
/// </summary>
public static class TaxonomyAnchors
{
    // Anchor IDs use a recognisable prefix (0x1A = 26 = 'Z' for "anchor")
    // so they're easy to spot in the database.

    // ── Snacks & Confectionery ─────────────────────────────────────
    public static readonly Guid TypeKeropokLekor          = Guid.Parse("1A000001-0000-0000-0000-000000000001");
    public static readonly Guid TypeMurukku              = Guid.Parse("1A000001-0000-0000-0000-000000000002");
    public static readonly Guid TypeKuehLapis            = Guid.Parse("1A000001-0000-0000-0000-000000000003");
    public static readonly Guid TypeTempehChips          = Guid.Parse("1A000001-0000-0000-0000-000000000004");
    public static readonly Guid TypeRengginang           = Guid.Parse("1A000001-0000-0000-0000-000000000005");
    public static readonly Guid TypeTauHuayCrisps         = Guid.Parse("1A000001-0000-0000-0000-000000000006");
    public static readonly Guid TypeBaklava              = Guid.Parse("1A000001-0000-0000-0000-000000000007");
    public static readonly Guid TypeNamkeenMix           = Guid.Parse("1A000001-0000-0000-0000-000000000008");
    public static readonly Guid TypePeanutBrittle        = Guid.Parse("1A000001-0000-0000-0000-000000000009");

    // ── Beverages ──────────────────────────────────────────────────
    public static readonly Guid TypeTehTarik             = Guid.Parse("1A000002-0000-0000-0000-000000000001");
    public static readonly Guid TypeKopiO                = Guid.Parse("1A000002-0000-0000-0000-000000000002");
    public static readonly Guid TypeSirapBandung         = Guid.Parse("1A000002-0000-0000-0000-000000000003");
    public static readonly Guid TypeKunyitAsam           = Guid.Parse("1A000002-0000-0000-0000-000000000004");
    public static readonly Guid TypeCoconutWater         = Guid.Parse("1A000002-0000-0000-0000-000000000005");
    public static readonly Guid TypeQahwaArabic          = Guid.Parse("1A000002-0000-0000-0000-000000000006");
    public static readonly Guid TypeRoseWaterDrink       = Guid.Parse("1A000002-0000-0000-0000-000000000007");
    public static readonly Guid TypeBandungConcentrate   = Guid.Parse("1A000002-0000-0000-0000-000000000008");

    // ── Meat & Poultry ────────────────────────────────────────────
    public static readonly Guid TypeAyamKampung          = Guid.Parse("1A000003-0000-0000-0000-000000000001");
    public static readonly Guid TypeDagingKambing        = Guid.Parse("1A000003-0000-0000-0000-000000000002");
    public static readonly Guid TypeSateAyam             = Guid.Parse("1A000003-0000-0000-0000-000000000003");
    public static readonly Guid TypeRendangDaging        = Guid.Parse("1A000003-0000-0000-0000-000000000004");
    public static readonly Guid TypeChickenNugget        = Guid.Parse("1A000003-0000-0000-0000-000000000005");
    public static readonly Guid TypeLambMince            = Guid.Parse("1A000003-0000-0000-0000-000000000006");
    public static readonly Guid TypeSosisSapi            = Guid.Parse("1A000003-0000-0000-0000-000000000007");
    public static readonly Guid TypeOtakOtak             = Guid.Parse("1A000003-0000-0000-0000-000000000008");

    // ── Sauces, Spices & Condiments ────────────────────────────────
    public static readonly Guid TypeRendangPaste         = Guid.Parse("1A000004-0000-0000-0000-000000000001");
    public static readonly Guid TypeSambalTerasi         = Guid.Parse("1A000004-0000-0000-0000-000000000002");
    public static readonly Guid TypeKariAyamPowder       = Guid.Parse("1A000004-0000-0000-0000-000000000003");
    public static readonly Guid TypeTurmericGround       = Guid.Parse("1A000004-0000-0000-0000-000000000004");
    public static readonly Guid TypeZaatarBlend          = Guid.Parse("1A000004-0000-0000-0000-000000000005");
    public static readonly Guid TypeBlackPepper          = Guid.Parse("1A000004-0000-0000-0000-000000000006");
    public static readonly Guid TypeCurryLeaf            = Guid.Parse("1A000004-0000-0000-0000-000000000007");
    public static readonly Guid TypeBumbuNasiGoreng      = Guid.Parse("1A000004-0000-0000-0000-000000000008");

    // ── Bakery ─────────────────────────────────────────────────────
    public static readonly Guid TypeRotiCanai            = Guid.Parse("1A000005-0000-0000-0000-000000000001");
    public static readonly Guid TypeNaanGarlic           = Guid.Parse("1A000005-0000-0000-0000-000000000002");
    public static readonly Guid TypeWholemealBread      = Guid.Parse("1A000005-0000-0000-0000-000000000003");
    public static readonly Guid TypeKuihBahulu           = Guid.Parse("1A000005-0000-0000-0000-000000000004");
    public static readonly Guid TypeBlueberryMuffin      = Guid.Parse("1A000005-0000-0000-0000-000000000005");
    public static readonly Guid TypeDateCookies          = Guid.Parse("1A000005-0000-0000-0000-000000000006");
    public static readonly Guid TypePiaDurian            = Guid.Parse("1A000005-0000-0000-0000-000000000007");
    public static readonly Guid TypeMartabakManis        = Guid.Parse("1A000005-0000-0000-0000-000000000008");

    // ── Skincare / Haircare ────────────────────────────────────────
    public static readonly Guid TypeVitaminCSerum        = Guid.Parse("1A000006-0000-0000-0000-000000000001");
    public static readonly Guid TypeSpirulinaMask        = Guid.Parse("1A000006-0000-0000-0000-000000000002");
    public static readonly Guid TypeAloeMoisturizer      = Guid.Parse("1A000006-0000-0000-0000-000000000003");
    public static readonly Guid TypeRoseToner            = Guid.Parse("1A000006-0000-0000-0000-000000000004");
    public static readonly Guid TypePalmShampoo          = Guid.Parse("1A000007-0000-0000-0000-000000000001");
    public static readonly Guid TypeArganHairOil         = Guid.Parse("1A000007-0000-0000-0000-000000000002");
    public static readonly Guid TypeHijabConditioner     = Guid.Parse("1A000007-0000-0000-0000-000000000003");

    // ── Supplements / Health ───────────────────────────────────────
    public static readonly Guid TypeTualangHoney         = Guid.Parse("1A000008-0000-0000-0000-000000000001");
    public static readonly Guid TypeHabbatusSauda        = Guid.Parse("1A000008-0000-0000-0000-000000000002");
    public static readonly Guid TypeMedjoolDates         = Guid.Parse("1A000008-0000-0000-0000-000000000003");

    /// <summary>Centralised map from anchor type id → a sensible name
    /// (used when we seed the anchor types into the taxonomy so the
    /// taxonomy has a real node at each anchor id).</summary>
    public static readonly IReadOnlyDictionary<Guid, (string Name, string Slug, string SubSlug, string CatSlug, string DeptSlug)> AnchorMap = new Dictionary<Guid, (string, string, string, string, string)>
    {
        // dept = food-beverage
        [TypeKeropokLekor]         = ("Keropok Lekor",          "keropok",          "chips",         "snacks-confectionery", "food-beverage"),
        [TypeMurukku]             = ("Murukku",               "murukku",          "chips",         "snacks-confectionery", "food-beverage"),
        [TypeKuehLapis]           = ("Kueh Lapis",            "kueh-lapis",       "traditional-desserts", "bakery",  "food-beverage"),
        [TypeTempehChips]         = ("Tempeh Chips",          "tempeh-chips",     "chips",         "snacks-confectionery", "food-beverage"),
        [TypeRengginang]          = ("Rengginang",            "rengginang",       "chips",         "snacks-confectionery", "food-beverage"),
        [TypeTauHuayCrisps]       = ("Tau Huay Crisps",       "tau-huay",         "chips",         "snacks-confectionery", "food-beverage"),
        [TypeBaklava]             = ("Baklava",               "baklava",          "traditional-desserts", "bakery",  "food-beverage"),
        [TypeNamkeenMix]          = ("Namkeen Mix",           "namkeen",          "mixed-nuts",    "snacks-confectionery", "food-beverage"),
        [TypePeanutBrittle]       = ("Peanut Brittle",        "peanut-brittle",   "mixed-nuts",    "snacks-confectionery", "food-beverage"),

        [TypeTehTarik]            = ("Teh Tarik",             "teh-tarik",        "tea",           "beverages",    "food-beverage"),
        [TypeKopiO]               = ("Kopi O",                "kopi-o",           "coffee",        "beverages",    "food-beverage"),
        [TypeSirapBandung]        = ("Sirap Bandung",         "sirap-bandung",    "syrups",        "beverages",    "food-beverage"),
        [TypeKunyitAsam]          = ("Kunyit Asam",           "kunyit-asam",      "herbal-tea",    "beverages",    "food-beverage"),
        [TypeCoconutWater]        = ("Coconut Water",         "coconut-water",    "coconut-water", "beverages",    "food-beverage"),
        [TypeQahwaArabic]         = ("Qahwa Arabic Coffee",   "qahwa",            "arabic",        "beverages",    "food-beverage"),
        [TypeRoseWaterDrink]      = ("Rose Water Drink",      "rose-water",       "rose",          "beverages",    "food-beverage"),
        [TypeBandungConcentrate]  = ("Bandung Concentrate",   "bandung",          "concentrate",   "beverages",    "food-beverage"),

        [TypeAyamKampung]         = ("Ayam Kampung",          "ayam-kampung",     "whole",         "chicken",      "meat-poultry"),
        [TypeDagingKambing]       = ("Daging Kambing",        "daging-kambing",   "cuts",          "goat",         "meat-poultry"),
        [TypeSateAyam]            = ("Sate Ayam",             "sate-ayam",        "marinated",     "chicken",      "meat-poultry"),
        [TypeRendangDaging]       = ("Rendang Daging",        "rendang-daging",   "frozen",        "beef",         "meat-poultry"),
        [TypeChickenNugget]       = ("Chicken Nugget",        "chicken-nugget",   "nuggets",       "chicken",      "meat-poultry"),
        [TypeLambMince]           = ("Lamb Mince",            "lamb-mince",       "minced",        "lamb-mutton",  "meat-poultry"),
        [TypeSosisSapi]           = ("Sosis Sapi",            "sosis-sapi",       "sausages",      "beef",         "meat-poultry"),
        [TypeOtakOtak]            = ("Otak-Otak",             "otak-otak",        "canned",        "fish",         "seafood"),

        [TypeRendangPaste]        = ("Rendang Paste",         "rendang-paste",    "sambal",        "sauces",       "sauces-spices"),
        [TypeSambalTerasi]        = ("Sambal Terasi",         "sambal-terasi",    "sambal",        "sauces",       "sauces-spices"),
        [TypeKariAyamPowder]      = ("Kari Ayam Powder",      "kari-ayam",        "curry-powder",  "spices",       "sauces-spices"),
        [TypeTurmericGround]      = ("Turmeric Ground",       "turmeric",         "turmeric",      "spices",       "sauces-spices"),
        [TypeZaatarBlend]         = ("Za'atar Blend",         "zaatar",           "zaatar",        "spices",       "sauces-spices"),
        [TypeBlackPepper]         = ("Black Pepper",          "black-pepper",     "black-pepper",  "spices",       "sauces-spices"),
        [TypeCurryLeaf]           = ("Curry Leaf",            "curry-leaf",       "leaves",        "spices",       "sauces-spices"),
        [TypeBumbuNasiGoreng]     = ("Bumbu Nasi Goreng",     "bumbu-nasi",       "paste",         "sauces",       "sauces-spices"),

        [TypeRotiCanai]           = ("Roti Canai",            "roti-canai",       "flatbread",     "bread",        "bakery"),
        [TypeNaanGarlic]          = ("Naan Garlic",           "naan",             "flatbread",     "bread",        "bakery"),
        [TypeWholemealBread]     = ("Wholemeal Bread",       "wholemeal",        "sliced",        "bread",        "bakery"),
        [TypeKuihBahulu]          = ("Kuih Bahulu",           "kuih-bahulu",      "traditional-desserts", "bakery", "food-beverage"),
        [TypeBlueberryMuffin]     = ("Blueberry Muffin",      "muffin",           "cupcakes",      "cakes",        "bakery"),
        [TypeDateCookies]         = ("Date Cookies",          "date-cookies",     "filled",        "biscuits",     "snacks-confectionery"),
        [TypePiaDurian]           = ("Pia Durian",            "pia-durian",       "traditional-desserts", "bakery", "food-beverage"),
        [TypeMartabakManis]       = ("Martabak Manis",        "martabak",         "traditional-desserts", "bakery", "food-beverage"),

        // dept = personal-care
        [TypeVitaminCSerum]       = ("Vitamin C Serum",       "vitamin-c",        "serums",        "skincare",     "personal-care"),
        [TypeSpirulinaMask]       = ("Spirulina Mask",        "spirulina-mask",   "face-masks",    "skincare",     "personal-care"),
        [TypeAloeMoisturizer]     = ("Aloe Moisturizer",      "aloe",             "moisturizers",  "skincare",     "personal-care"),
        [TypeRoseToner]           = ("Rose Toner",            "rose-toner",       "toners",        "skincare",     "personal-care"),
        [TypePalmShampoo]         = ("Palm Shampoo",          "palm-shampoo",     "shampoo",       "shampoo-conditioner", "personal-care"),
        [TypeArganHairOil]        = ("Argan Hair Oil",        "argan-oil",        "hair-oils",     "treatments-styling", "personal-care"),
        [TypeHijabConditioner]    = ("Hijab Conditioner",     "hijab-cond",       "conditioner",   "shampoo-conditioner", "personal-care"),

        // dept = health-wellness
        [TypeTualangHoney]        = ("Tualang Honey",         "tualang-honey",    "honey",         "herbal-supps", "health-wellness"),
        [TypeHabbatusSauda]       = ("Habbatus Sauda",        "habbatus-sauda",   "habbatus-sauda","herbal-supps", "health-wellness"),
        [TypeMedjoolDates]        = ("Medjool Dates",         "medjool-dates",    "medjool",       "dates-dried",  "fresh-produce"),
    };
}
