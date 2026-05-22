using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

public interface IClienteRepository
{
    Task<ClienteResolucionDto> ObtenerClientePorGuidAsync(string guidCliente, CancellationToken ct = default);
    Task<IReadOnlyList<ClienteResolucionDto>> ObtenerClientesBatchAsync(IEnumerable<string> guidsCliente, CancellationToken ct = default);
}
