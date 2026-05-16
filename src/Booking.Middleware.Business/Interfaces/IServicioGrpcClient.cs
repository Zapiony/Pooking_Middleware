using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Abstracción del cliente gRPC hacia el microservicio de Servicio.
/// Implementada en DataAccess/GrpcClients/ServicioGrpcClient.cs
/// </summary>
public interface IServicioGrpcClient
{
    Task<ServicioResolucionDto> ObtenerServicioPorGuidAsync(
        string guidServicio,
        CancellationToken ct = default);

    Task<DisponibilidadDto> ValidarDisponibilidadAsync(
        string guidServicio,
        string fechaInicio,
        string fechaFin,
        CancellationToken ct = default);

    Task<IReadOnlyList<ServicioResolucionDto>> ObtenerServiciosBatchAsync(
        IEnumerable<string> guidsServicio,
        CancellationToken ct = default);
}
