using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

public interface IServicioRepository
{
    Task<ServicioResolucionDto> ObtenerServicioPorGuidAsync(string guidServicio, CancellationToken ct = default);
    Task<DisponibilidadDto> ValidarDisponibilidadAsync(string guidServicio, string fechaInicio, string fechaFin, CancellationToken ct = default);
    Task<IReadOnlyList<ServicioResolucionDto>> ObtenerServiciosBatchAsync(IEnumerable<string> guidsServicio, CancellationToken ct = default);
}
