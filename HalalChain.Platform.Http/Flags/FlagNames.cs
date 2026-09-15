namespace HalalChain.Platform.Http.Flags;

public static class FlagNames
{
    public const string WishlistApiBacked = "services.wishlist.api_backed";
    public const string CartApiBacked     = "services.cart.api_backed";
    // Cart router keys on auth, not flag — this flag is reserved for future "server cart for anonymous" if ever needed.
    public const string CartMergeEnabled  = "services.cart.merge_enabled";
}