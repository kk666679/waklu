namespace HalalChain.Localization;

public static class SupportedCultures
{
    public static readonly IReadOnlyList<CultureDescriptor> All = new[]
    {
        new CultureDescriptor("en",      "English",            "English",          "\U0001F1EC\U0001F1E7", "ltr", "USD",   "Global"),
        new CultureDescriptor("ms",      "Malay",              "Bahasa Melayu",    "\U0001F1F2\U0001F1FE", "ltr", "MYR",   "ASEAN · Malaysia, Brunei, Singapore"),
        new CultureDescriptor("id",      "Indonesian",         "Bahasa Indonesia", "\U0001F1EE\U0001F1E9", "ltr", "IDR",   "ASEAN · Indonesia"),
        new CultureDescriptor("th",      "Thai",               "ภาษาไทย",          "\U0001F1F9\U0001F1ED", "ltr", "THB",   "ASEAN · Thailand"),
        new CultureDescriptor("vi",      "Vietnamese",         "Tiếng Việt",       "\U0001F1FB\U0001F1F3", "ltr", "VND",   "ASEAN · Vietnam"),
        new CultureDescriptor("tl",      "Filipino",           "Filipino",         "\U0001F1F5\U0001F1ED", "ltr", "PHP",   "ASEAN · Philippines"),
        new CultureDescriptor("zh-Hans", "Chinese (Simplified)","简体中文",         "\U0001F1E8\U0001F1F3", "ltr", "CNY",   "ASEAN+ · Singapore, China diaspora"),
        new CultureDescriptor("ar",      "Arabic",             "العربية",          "\U0001F1F8\U0001F1E6", "rtl", "AED",   "MENA · Halal origin")
    };

    public const string DefaultCulture = "en";
    public const string CookieName = "hc.culture";
    public const string QueryName = "culture";

    public static CultureDescriptor? Find(string id) =>
        All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    public static bool IsSupported(string id) => Find(id) is not null;
}
