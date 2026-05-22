using Booking.Auditoria.API.Protos;
using Booking.Middleware.Business.Interfaces;
using Booking.Middleware.DataAccess.GrpcClients;
using Microservicio.Pooking.Auth.Api.Protos;
using Microservicio.Pooking.Cliente.Api.Protos;
using Microservicio.Pooking.Servicio.Api.Protos;

namespace Booking.Middleware.Api.Extensions;

/// <summary>
/// Registra los clientes gRPC hacia los microservicios externos y sus abstracciones.
/// Llamado desde Program.cs con builder.Services.AddMiddlewareGrpcClients(configuration).
/// </summary>
public static class GrpcClientsExtensions
{
    public static IServiceCollection AddMiddlewareGrpcClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Auditoria gRPC Client ─────────────────────────────────────────────
        var auditoriaEndpoint = configuration["GrpcEndpoints:Auditoria"];
        var auditoriaEnabled = !string.IsNullOrWhiteSpace(auditoriaEndpoint) && auditoriaEndpoint != "disabled";

        if (auditoriaEnabled)
        {
            services.AddGrpcClient<AuditoriaGrpcService.AuditoriaGrpcServiceClient>(o =>
            {
                o.Address = new Uri(auditoriaEndpoint!);
            });
            services.AddScoped<IAuditoriaGrpcClient, AuditoriaGrpcClient>();
        }
        else
        {
            services.AddScoped<IAuditoriaGrpcClient, AuditoriaGrpcClientDisabled>();
        }

        // ── Auth gRPC Client ──────────────────────────────────────────────────
        var authEndpoint = configuration["GrpcEndpoints:Auth"]
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Auth en configuración.");

        services.AddGrpcClient<AuthGrpc.AuthGrpcClient>(o =>
        {
            o.Address = new Uri(authEndpoint);
        });
        services.AddScoped<IAuthGrpcClient, AuthGrpcClient>();

        // ── Servicio gRPC Client ──────────────────────────────────────────────
        var servicioEndpoint = configuration["GrpcEndpoints:Servicio"]
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Servicio en configuración.");

        services.AddGrpcClient<ServicioGrpc.ServicioGrpcClient>(o =>
        {
            o.Address = new Uri(servicioEndpoint);
        });
        services.AddScoped<IServicioGrpcClient, ServicioGrpcClient>();

        // ── Cliente gRPC Client ───────────────────────────────────────────────
        var clienteEndpoint = configuration["GrpcEndpoints:Cliente"]
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Cliente en configuración.");

        services.AddGrpcClient<ClienteGrpc.ClienteGrpcClient>(o =>
        {
            o.Address = new Uri(clienteEndpoint);
        });
        services.AddScoped<IClienteGrpcClient, ClienteGrpcClient>();

        return services;
    }
}

/// <summary>
/// Implementación mock/No-op para cuando el servicio de Auditoria está deshabilitado en desarrollo local.
/// Evita errores en cascada y excepciones no controladas cuando los servicios envían eventos de auditoría.
/// </summary>
internal sealed class AuditoriaGrpcClientDisabled : IAuditoriaGrpcClient
{
    private readonly ILogger<AuditoriaGrpcClientDisabled> _logger;

    public AuditoriaGrpcClientDisabled(ILogger<AuditoriaGrpcClientDisabled> logger)
    {
        _logger = logger;
    }

    public Task<(bool Success, long IdLogAuditoria, string Mensaje)> RegistrarEventoAsync(
        Booking.Middleware.Business.DTOs.EventoDto evento,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Auditoria MOCK: RegistrarEvento[{Operacion}] en tabla [{Tabla}] (IdCorrelacion={IdCorr}) exitoso.",
            evento.Operacion, evento.TablaAfectada, evento.IdCorrelacion);
        return Task.FromResult((true, 999L, "Auditoría deshabilitada (Mock exitoso)"));
    }

    public Task<(int Exitosos, int Fallidos)> RegistrarEventosAsync(
        IEnumerable<Booking.Middleware.Business.DTOs.EventoDto> eventos,
        CancellationToken ct = default)
    {
        var list = eventos.ToList();
        _logger.LogInformation(
            "Auditoria MOCK: RegistrarEventos lote de {Count} eventos exitoso.",
            list.Count);
        return Task.FromResult((list.Count, 0));
    }
}

