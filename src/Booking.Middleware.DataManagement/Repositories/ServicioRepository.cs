using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;

namespace Booking.Middleware.DataManagement.Repositories;

/// <summary>
/// Repositorio sobre el cliente gRPC de Servicio.
/// Permite agregar caché corta de catálogo (p.ej. IMemoryCache) sin afectar capas superiores.
/// </summary>
public sealed class ServicioRepository
{
    private readonly IServicioGrpcClient _client;

    public ServicioRepository(IServicioGrpcClient client)
    {
        _client = client;
    }

    public Task<ServicioResolucionDto> ObtenerServicioPorGuidAsync(
        string guidServicio,
        CancellationToken ct = default)
        => _client.ObtenerServicioPorGuidAsync(guidServicio, ct);

    public Task<DisponibilidadDto> ValidarDisponibilidadAsync(
        string guidServicio,
        string fechaInicio,
        string fechaFin,
        CancellationToken ct = default)
        => _client.ValidarDisponibilidadAsync(guidServicio, fechaInicio, fechaFin, ct);

    public Task<IReadOnlyList<ServicioResolucionDto>> ObtenerServiciosBatchAsync(
        IEnumerable<string> guidsServicio,
        CancellationToken ct = default)
        => _client.ObtenerServiciosBatchAsync(guidsServicio, ct);
}
