using System.Diagnostics;
using System.Net.Http.Headers;

namespace HalalChain.Web.Infrastructure;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}

public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; } = Activity.Current?.Id ?? Guid.NewGuid().ToString();
}

public sealed class CorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationContext _correlation;

    public CorrelationIdHandler(ICorrelationContext correlation)
    {
        _correlation = correlation;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Correlation-ID", _correlation.CorrelationId);
        return base.SendAsync(request, cancellationToken);
    }
}
