using Booking.Middleware.Api.Services;

namespace Booking.Middleware.Api.Extensions;

/// <summary>
/// Registra los servicios gRPC server del Middleware.
/// Llamado desde Program.cs con app.MapMiddlewareGrpcServices().
/// </summary>
public static class GrpcServerExtensions
{
    public static WebApplication MapMiddlewareGrpcServices(this WebApplication app)
    {
        app.MapGrpcService<EventBusGrpcHandler>();
        app.MapGrpcService<ResolverGrpcHandler>();
        return app;
    }
}
