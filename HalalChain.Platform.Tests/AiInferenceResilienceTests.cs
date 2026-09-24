using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using HalalChain.Platform.Api.AI;
using HalalChain.Platform.Contracts.AI.Requests;
using Microsoft.Extensions.Options;

namespace HalalChain.Platform.Tests;

public sealed class AiInferenceResilienceTests
{
    [Fact]
    public async Task ClassifyAsync_FallsBackToSecondaryProvider_WhenPrimaryIsUnavailable()
    {
        var primary = new HttpClient(new StubHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("primary unavailable", Encoding.UTF8, "text/plain")
            }))
        {
            BaseAddress = new Uri("http://primary.local")
        };

        var secondary = new HttpClient(new StubHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"model\":\"fallback-model\",\"bestLabel\":\"halal\",\"bestScore\":0.91,\"scores\":{\"halal\":0.91,\"haram\":0.09}}",
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri("http://secondary.local")
        };

        var provider = new ResilientAiInferenceProvider(primary, secondary, Options.Create(new AiGatewayOptions
        {
            BaseUrl = "http://primary.local",
            FallbackBaseUrl = "http://secondary.local"
        }));

        var result = await provider.ClassifyAsync(new ClassifyRequest("milk", ["halal", "haram"]), CancellationToken.None);

        result.BestLabel.Should().Be("halal");
        result.Model.Should().Be("fallback-model");
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
