using Booking.Middleware.Business.Services;
using Booking.Middleware.Business.Interfaces;
using Booking.Middleware.DataManagement.Repositories;

namespace Booking.Middleware.Api.Extensions;

/// <summary>
/// Registra las capas Business y DataManagement del Middleware.
/// Llamado desde Program.cs con builder.Services.AddMiddlewareServices().
/// </summary>
public static class MiddlewareServicesExtensions
{
    public static IServiceCollection AddMiddlewareServices(this IServiceCollection services)
    {
        // ── DataManagement — repositorios ─────────────────────────────────────
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IServicioRepository, ServicioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();

        // ── Business — servicios ──────────────────────────────────────────────
        services.AddScoped<IEventBusService, EventBusService>();
        services.AddScoped<ICrossDomainResolverService, CrossDomainResolverService>();

        return services;
    }
}
