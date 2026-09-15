using HalalChain.Platform.Http.Models;

namespace HalalChain.Platform.Http.Abstractions;

public interface IApiClient
{
    Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default);
    Task<Result<T>> GetAsync<T>(string path, object query, CancellationToken ct = default);

    Task<Result<T>> PostAsync<T>(string path, object body,
        string? idempotencyKey = null, CancellationToken ct = default);
    Task<Result> PostAsync(string path, object body,
        string? idempotencyKey = null, CancellationToken ct = default);

    Task<Result<T>> PutAsync<T>(string path, object body, CancellationToken ct = default);
    Task<Result<T>> PatchAsync<T>(string path, object body, CancellationToken ct = default);
    Task<Result> DeleteAsync(string path, CancellationToken ct = default);
}