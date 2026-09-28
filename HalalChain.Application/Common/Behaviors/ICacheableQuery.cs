namespace HalalChain.Application.Common.Behaviors;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
    TimeSpan LocalExpiration { get; }
    string[] Tags { get; }
}
