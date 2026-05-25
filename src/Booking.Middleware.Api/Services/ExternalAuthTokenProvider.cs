using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Booking.Middleware.Api.Services;

/// <summary>
/// Provee bearer tokens cacheados para clusters externos que requieren auth.
/// Hace login automáticamente cuando el token no está cacheado o expiró.
/// </summary>
public interface IExternalAuthTokenProvider
{
    Task<string?> GetTokenAsync(
        string clusterId,
        string destinationBaseUrl,
        string username,
        string password,
        CancellationToken ct = default);

    void InvalidateToken(string clusterId);
}

public sealed class ExternalAuthTokenProvider : IExternalAuthTokenProvider
{
    private const string LoginPath = "/api/v1/auth/login";
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(50);
    private static readonly TimeSpan ExpirationMargin = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExternalAuthTokenProvider> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public ExternalAuthTokenProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ExternalAuthTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync(
        string clusterId,
        string destinationBaseUrl,
        string username,
        string password,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue<string>(CacheKey(clusterId), out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        // Lock por cluster para evitar dogpile en logins concurrentes
        var gate = _locks.GetOrAdd(clusterId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // Double-check después de adquirir el lock
            if (_cache.TryGetValue<string>(CacheKey(clusterId), out cached) && !string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            var (token, ttl) = await LoginAsync(clusterId, destinationBaseUrl, username, password, ct);
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            _cache.Set(CacheKey(clusterId), token, ttl);
            _logger.LogInformation(
                "ExternalAuth: Token cacheado para cluster {ClusterId} con TTL {TtlMinutes} min.",
                clusterId, (int)ttl.TotalMinutes);

            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    public void InvalidateToken(string clusterId)
    {
        _cache.Remove(CacheKey(clusterId));
        _logger.LogInformation("ExternalAuth: Token invalidado para cluster {ClusterId}.", clusterId);
    }

    private async Task<(string? Token, TimeSpan Ttl)> LoginAsync(
        string clusterId,
        string destinationBaseUrl,
        string username,
        string password,
        CancellationToken ct)
    {
        var loginUrl = $"{destinationBaseUrl.TrimEnd('/')}{LoginPath}";
        var httpClient = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            _logger.LogInformation(
                "ExternalAuth: Login en cluster {ClusterId} -> {Url}.", clusterId, loginUrl);

            response = await httpClient.PostAsJsonAsync(loginUrl, new { username, password }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ExternalAuth: Error de conexión al hacer login en cluster {ClusterId}.", clusterId);
            return (null, FallbackTtl);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "ExternalAuth: Login falló en cluster {ClusterId} con estado {Status}. Body: {Body}",
                clusterId, response.StatusCode, body);
            return (null, FallbackTtl);
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("data", out var data))
            {
                _logger.LogWarning(
                    "ExternalAuth: Respuesta de login en cluster {ClusterId} sin propiedad 'data'.", clusterId);
                return (null, FallbackTtl);
            }

            if (!data.TryGetProperty("token", out var tokenProp) || tokenProp.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning(
                    "ExternalAuth: Respuesta de login en cluster {ClusterId} sin 'data.token' válido.", clusterId);
                return (null, FallbackTtl);
            }

            var token = tokenProp.GetString();
            if (string.IsNullOrWhiteSpace(token))
            {
                return (null, FallbackTtl);
            }

            var ttl = ResolveTtl(data);
            return (token, ttl);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "ExternalAuth: Respuesta de login en cluster {ClusterId} no es JSON válido.", clusterId);
            return (null, FallbackTtl);
        }
    }

    private static TimeSpan ResolveTtl(JsonElement data)
    {
        if (data.TryGetProperty("expiration", out var expProp) &&
            expProp.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(expProp.GetString(), out var expiration))
        {
            var remaining = expiration - DateTimeOffset.UtcNow - ExpirationMargin;
            if (remaining > TimeSpan.FromMinutes(1))
            {
                return remaining;
            }
        }

        return FallbackTtl;
    }

    private static string CacheKey(string clusterId) => $"external-auth-token::{clusterId}";
}
