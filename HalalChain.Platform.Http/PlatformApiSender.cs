using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HalalChain.Platform.Http.Abstractions;
using HalalChain.Platform.Http.Models;

namespace HalalChain.Platform.Http;

/// <summary>
/// Low-level transport for the platform API. Each host wraps this with its own
/// strongly-typed surface; this class only knows how to attach auth, send, and
/// shape the response into <see cref="ApiResult{T}"/>.
/// </summary>
public sealed class PlatformApiSender
{
    private readonly HttpClient _http;
    private readonly IPlatformTokenAccessor _tokenAccessor;
    private readonly JsonSerializerOptions _json;

    public PlatformApiSender(
        HttpClient http,
        IPlatformTokenAccessor tokenAccessor,
        JsonSerializerOptions? json = null)
    {
        _http = http;
        _tokenAccessor = tokenAccessor;
        _json = json ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, path, content: null, ct);

    public async Task<ApiResult<T>> PostAsync<T>(string path, object? body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Post, path, body, ct);

    public async Task<ApiResult<T>> PatchAsync<T>(string path, object? body, CancellationToken ct = default)
        => await SendAsync<T>(new HttpMethod("PATCH"), path, body, ct);

    public async Task<ApiResult<T>> PutAsync<T>(string path, object? body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Put, path, body, ct);

    public async Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Delete, path, content: null, ct);

    public async Task<ApiResult<bool>> DeleteAsync(string path, CancellationToken ct = default)
    {
        var result = await SendAsync<object>(HttpMethod.Delete, path, content: null, ct);
        return new ApiResult<bool>(result.IsSuccess, result.IsSuccess, result.Status, result.Error);
    }

    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
        => await SendAsync<TResponse>(HttpMethod.Post, path, body, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? content, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            await AttachAuthAsync(request, ct);

            if (content is not null)
            {
                var json = JsonSerializer.Serialize(content, _json);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await _http.SendAsync(request, ct);
            return await ShapeAsync<T>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail(ApiStatus.Offline, $"Platform API is unreachable: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<T>.Fail(ApiStatus.Offline, "Request timed out.");
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail(ApiStatus.ServerError, ex.Message);
        }
    }

    private async Task<ApiResult<T>> ShapeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var status = MapStatus(response.StatusCode);
        var stream = await response.Content.ReadAsStreamAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            if (status == ApiStatus.Empty || response.Content.Headers.ContentLength is 0)
            {
                return ApiResult<T>.Empty();
            }

            var data = await JsonSerializer.DeserializeAsync<T>(stream, _json, ct);
            return data is null
                ? ApiResult<T>.Empty()
                : ApiResult<T>.Ok(data);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        return ApiResult<T>.Fail(status, body);
    }

    private static ApiStatus MapStatus(HttpStatusCode code) => code switch
    {
        HttpStatusCode.OK => ApiStatus.Ok,
        HttpStatusCode.NoContent => ApiStatus.Empty,
        HttpStatusCode.Unauthorized => ApiStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiStatus.Forbidden,
        HttpStatusCode.NotFound => ApiStatus.NotFound,
        HttpStatusCode.UnprocessableEntity => ApiStatus.ValidationError,
        HttpStatusCode.BadRequest => ApiStatus.Error,
        _ => ApiStatus.ServerError
    };

    private async Task AttachAuthAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenAccessor.GetAccessTokenAsync(ct);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
