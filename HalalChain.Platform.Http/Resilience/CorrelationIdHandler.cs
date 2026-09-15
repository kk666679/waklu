using Microsoft.AspNetCore.Http;

namespace HalalChain.Platform.Http.Resilience;

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-Id";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var corrId = accessor.HttpContext?.TraceIdentifier
                     ?? System.Diagnostics.Activity.Current?.TraceId.ToString()
                     ?? Guid.NewGuid().ToString("N");

        if (!request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(HeaderName, corrId);

        return base.SendAsync(request, ct);
    }
}