using System.Net;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Booking.Middleware.Api.Services;

/// <summary>
/// Transform de YARP que inyecta un Bearer token en las requests hacia clusters externos
/// marcados con Metadata "Auth.Required" = "true". Las credenciales NO viven en yarp.json:
/// se resuelven desde IConfiguration en la sección ExternalAuth:{CredentialsKey}, que en
/// producción viene de variables de entorno (Azure App Settings) y en desarrollo local de
/// User Secrets. Invalida el cache de token ante un 401.
/// </summary>
public sealed class BearerInjectionTransformProvider : ITransformProvider
{
    private const string MetaRequired = "Auth.Required";
    private const string MetaCredentialsKey = "Auth.CredentialsKey";
    private const string ConfigSection = "ExternalAuth";

    private readonly IConfiguration _configuration;
    private readonly ILogger<BearerInjectionTransformProvider> _logger;

    public BearerInjectionTransformProvider(
        IConfiguration configuration,
        ILogger<BearerInjectionTransformProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        var meta = context.Cluster.Metadata;
        if (meta is null) return;
        if (!string.Equals(meta.GetValueOrDefault(MetaRequired), "true", StringComparison.OrdinalIgnoreCase))
            return;

        var credentialsKey = meta.GetValueOrDefault(MetaCredentialsKey);
        if (string.IsNullOrWhiteSpace(credentialsKey))
        {
            context.Errors.Add(new ArgumentException(
                $"El cluster '{context.Cluster.ClusterId}' tiene Auth.Required=true pero falta Auth.CredentialsKey."));
        }
    }

    public void Apply(TransformBuilderContext context)
    {
        var cluster = context.Cluster;
        var meta = cluster?.Metadata;
        if (cluster is null || meta is null) return;
        if (!string.Equals(meta.GetValueOrDefault(MetaRequired), "true", StringComparison.OrdinalIgnoreCase))
            return;

        var credentialsKey = meta.GetValueOrDefault(MetaCredentialsKey);
        if (string.IsNullOrWhiteSpace(credentialsKey))
            return;

        var (username, password) = ResolveCredentials(credentialsKey);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError(
                "ExternalAuth: Credenciales no configuradas para cluster {ClusterId} (clave '{Key}'). " +
                "Definir env vars {SectionName}__{Key}__Username y {SectionName}__{Key}__Password.",
                cluster.ClusterId, credentialsKey, ConfigSection, credentialsKey);
            return;
        }

        var clusterId = cluster.ClusterId;
        var destinationBaseUrl = cluster.Destinations?.Values.FirstOrDefault()?.Address;
        if (string.IsNullOrWhiteSpace(destinationBaseUrl))
            return;

        // Request transform: inyectar Authorization: Bearer {token}
        context.AddRequestTransform(async transformCtx =>
        {
            var httpCtx = transformCtx.HttpContext;
            var provider = httpCtx.RequestServices.GetRequiredService<IExternalAuthTokenProvider>();
            var ct = httpCtx.RequestAborted;

            var token = await provider.GetTokenAsync(clusterId, destinationBaseUrl, username, password, ct);
            if (!string.IsNullOrEmpty(token))
            {
                transformCtx.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        });

        // Response transform: si el downstream devuelve 401, invalidar el cache
        context.AddResponseTransform(transformCtx =>
        {
            if (transformCtx.ProxyResponse?.StatusCode == HttpStatusCode.Unauthorized)
            {
                var provider = transformCtx.HttpContext.RequestServices
                    .GetRequiredService<IExternalAuthTokenProvider>();
                provider.InvalidateToken(clusterId);
            }
            return ValueTask.CompletedTask;
        });
    }

    private (string? Username, string? Password) ResolveCredentials(string credentialsKey)
    {
        var section = _configuration.GetSection($"{ConfigSection}:{credentialsKey}");
        return (section["Username"], section["Password"]);
    }
}
