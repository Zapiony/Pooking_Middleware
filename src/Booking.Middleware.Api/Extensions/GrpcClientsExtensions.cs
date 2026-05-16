using Booking.Auditoria.API.Protos;
using Booking.Middleware.Business.Interfaces;
using Booking.Middleware.DataAccess.GrpcClients;
using Microservicio.Pooking.Auth.Api.Protos;
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
        var auditoriaEndpoint = configuration["GrpcEndpoints:Auditoria"]
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Auditoria en configuración.");

        services.AddGrpcClient<AuditoriaGrpcService.AuditoriaGrpcServiceClient>(o =>
        {
            o.Address = new Uri(auditoriaEndpoint);
        });
        services.AddScoped<IAuditoriaGrpcClient, AuditoriaGrpcClient>();

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

        return services;
    }
}
