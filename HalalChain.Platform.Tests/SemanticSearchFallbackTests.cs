using System.Net;
using System.Net.Http;
using FluentAssertions;
using HalalChain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HalalChain.Platform.Tests;

public sealed class SemanticSearchFallbackTests
{
    [Fact]
    public async Task SemanticSearchAsync_WhenApiIsUnavailable_ReturnsEmptyArray()
    {
        var client = new PlatformApiClient(
            new StubHttpClientFactory(new HttpClient(new FailingHandler())),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build(),
            NullLogger<PlatformApiClient>.Instance,
            new StubAuthService());

        var result = await client.SemanticSearchAsync("coconut", topK: 5);

        result.Should().BeEmpty();
    }

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("search API unavailable");
    }

    private sealed class StubAuthService : IAuthService
    {
        public event Action? OnAuthStateChanged;
        public bool IsAuthenticated => false;
        public string? UserName => null;
        public string? FullName => null;
        public string? Email => null;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
        public string? Token => null;

        public Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default) => Task.FromResult(new AuthResult(false));
        public Task<AuthResult> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default) => Task.FromResult(new AuthResult(false));
        public Task LogoutAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task<string?> GetTokenAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    }
}
