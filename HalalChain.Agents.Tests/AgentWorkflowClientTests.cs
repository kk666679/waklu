using System.Net;
using System.Text;

using HalalChain.Agents.Runtime;
using HalalChain.Application.Agentic.Models;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace HalalChain.Agents.Tests;

public sealed class AgentWorkflowClientTests
{
    private static AgentWorkflowClient Client(HttpMessageHandler handler, params string[] allowed) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://agents.test") },
            Options.Create(new AgentsRuntimeOptions
            {
                BaseUrl = "http://agents.test",
                AllowedWorkflows = allowed.Length == 0 ? ["supplier_onboarding"] : [.. allowed],
            }),
            NullLogger<AgentWorkflowClient>.Instance);

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }

    [Fact]
    public async Task RunAsync_MapsProposalsOntoEvidenceProposal()
    {
        const string body = """
            {"runId":"r1","proposals":[
              {"kind":"Certificate","summary":"JAKIM cert found","confidence":0.92,
               "sourceRefs":[{"kind":"issuer_api","reference":"JAKIM/123","retrievedAt":null,"note":null}],
               "evidence":{"certificate_number":"123"}}]}
            """;

        var proposals = await Client(new StubHandler(HttpStatusCode.OK, body))
            .RunAsync("supplier_onboarding", new { vendorId = "v1" }, TestContext.Current.CancellationToken);

        var proposal = Assert.Single(proposals);
        Assert.Equal(EvidenceKind.Certificate, proposal.Kind);
        Assert.Equal(0.92, proposal.Confidence);
        Assert.Equal("JAKIM/123", Assert.Single(proposal.SourceRefs).Reference);
        Assert.True(proposal.ProposedEvidence.ContainsKey("certificate_number"));
    }

    [Fact]
    public async Task RunAsync_UnknownEvidenceKindFallsBackToOther()
    {
        // An unrecognized kind must not fail the run — losing a whole
        // workflow's evidence over one novel label is the wrong trade.
        const string body = """{"runId":"r1","proposals":[{"kind":"Xenoglyph","summary":"s","confidence":0.5}]}""";

        var proposal = Assert.Single(
            await Client(new StubHandler(HttpStatusCode.OK, body))
                .RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken));

        Assert.Equal(EvidenceKind.Other, proposal.Kind);
    }

    [Fact]
    public async Task RunAsync_DropsAVerdictIfTheServiceSendsOne()
    {
        // The agents service is untrusted at this boundary. A response that
        // smuggles in a verdict has it silently discarded: there is no field
        // for it to land in.
        const string body = """
            {"runId":"r1","verdict":"halal","complianceStatus":"VERIFIED",
             "proposals":[{"kind":"Certificate","summary":"s","confidence":0.9}]}
            """;

        var proposals = await Client(new StubHandler(HttpStatusCode.OK, body))
            .RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken);

        var proposal = Assert.Single(proposals);
        Assert.DoesNotContain("verdict", proposal.ProposedEvidence.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("complianceStatus", proposal.ProposedEvidence.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_RejectsAnUnregisteredWorkflow()
    {
        // Running a workflow with no goldens means its failures go unseen.
        var ex = await Assert.ThrowsAsync<AgentWorkflowNotAllowedException>(
            () => Client(new StubHandler(HttpStatusCode.OK, "{}"), "certificate_review")
                .RunAsync("not_registered", new { }, TestContext.Current.CancellationToken));

        Assert.Equal("not_registered", ex.Workflow);
    }

    [Fact]
    public async Task RunAsync_UnreachableServiceFailsRatherThanGuessing()
    {
        // No in-process fallback. An unreachable agents service must fail.
        await Assert.ThrowsAsync<AgentWorkflowUnavailableException>(
            () => Client(new ThrowingHandler()).RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_ServiceUnavailableIsAnAvailabilityFailure()
    {
        await Assert.ThrowsAsync<AgentWorkflowUnavailableException>(
            () => Client(new StubHandler(HttpStatusCode.ServiceUnavailable, "{}"))
                .RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_ServerErrorIsNotSwallowed()
    {
        await Assert.ThrowsAsync<HttpRequestException>(
            () => Client(new StubHandler(HttpStatusCode.InternalServerError, "{}"))
                .RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_MalformedBodyIsAnAvailabilityFailure()
    {
        await Assert.ThrowsAsync<AgentWorkflowUnavailableException>(
            () => Client(new StubHandler(HttpStatusCode.OK, "not json at all"))
                .RunAsync("supplier_onboarding", new { }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_PostsTheWorkflowAndInput()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"runId":"r1","proposals":[]}""");

        await Client(handler).RunAsync("supplier_onboarding", new { vendorId = "v1" }, TestContext.Current.CancellationToken);

        Assert.Contains("\"workflow\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"vendorId\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }
}
