using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;
using HalalChain.Platform.Http.Resilience;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HalalChain.Platform.Http.Http;

public sealed class ResilientApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IApiNotifier? _notifier;
    private readonly ILogger<ResilientApiClient> _logger;
    private readonly JsonSerializerOptions _json;

    public ResilientApiClient(
        HttpClient http,
        IOptions<JsonSerializerOptions> jsonOptions,
        ILogger<ResilientApiClient> logger,
        IApiNotifier? notifier = null)
    {
        _http = http;
        _logger = logger;
        _notifier = notifier;
        _json = jsonOptions.Value;
    }

    public async Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var result = await _http.GetAsync(path, ct);
        return await ProcessResponse<T>(result, ct);
    }

    public async Task<Result<T>> GetAsync<T>(string path, object query, CancellationToken ct = default)
    {
        var queryString = ToQueryString(query);
        var url = string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
        return await GetAsync<T>(url, ct);
    }

    public async Task<Result<T>> PostAsync<T>(string path, object body,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: _json);
        if (!string.IsNullOrEmpty(idempotencyKey))
            content.Headers.Add("Idempotency-Key", idempotencyKey);

        var result = await _http.PostAsync(path, content, ct);
        return await ProcessResponse<T>(result, ct);
    }

    public async Task<Result> PostAsync(string path, object body,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: _json);
        if (!string.IsNullOrEmpty(idempotencyKey))
            content.Headers.Add("Idempotency-Key", idempotencyKey);

        var result = await _http.PostAsync(path, content, ct);
        return await ProcessResponse(result, ct);
    }

    public async Task<Result<T>> PutAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: _json);
        var result = await _http.PutAsync(path, content, ct);
        return await ProcessResponse<T>(result, ct);
    }

    public async Task<Result<T>> PatchAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var content = JsonContent.Create(body, options: _json);
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), path) { Content = content };
        var result = await _http.SendAsync(request, ct);
        return await ProcessResponse<T>(result, ct);
    }

    public async Task<Result> DeleteAsync(string path, CancellationToken ct = default)
    {
        var result = await _http.DeleteAsync(path, ct);
        return await ProcessResponse(result, ct);
    }

    private async Task<Result<T>> ProcessResponse<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return Result<T>.Ok(default!);

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
            return Result<T>.Ok(data!);
        }

        var problem = await TryReadProblemDetails(response, ct);
        var error = ApiErrorMapper.From(response.StatusCode, problem);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await (_notifier?.NotifyAuthExpiredAsync(ct) ?? Task.CompletedTask);
        else
            await (_notifier?.NotifyErrorAsync(error.Message, ct) ?? Task.CompletedTask);

        _logger.LogWarning("API {Path} failed: {Status} {Code}", response.RequestMessage?.RequestUri?.PathAndQuery, response.StatusCode, error.Code);
        return Result<T>.Fail(error);
    }

    private async Task<Result> ProcessResponse(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return Result.Ok();

        var problem = await TryReadProblemDetails(response, ct);
        var error = ApiErrorMapper.From(response.StatusCode, problem);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await (_notifier?.NotifyAuthExpiredAsync(ct) ?? Task.CompletedTask);
        else
            await (_notifier?.NotifyErrorAsync(error.Message, ct) ?? Task.CompletedTask);

        _logger.LogWarning("API {Path} failed: {Status} {Code}", response.RequestMessage?.RequestUri?.PathAndQuery, response.StatusCode, error.Code);
        return Result.Fail(error);
    }

    private async Task<ProblemDetails?> TryReadProblemDetails(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, _json, ct);
        }
        catch { return null; }
    }

    private static string ToQueryString(object obj)
    {
        var props = obj.GetType().GetProperties();
        var parts = props
            .Where(p => p.GetValue(obj) != null)
            .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.GetValue(obj)!.ToString()!)}");
        return string.Join("&", parts);
    }
}