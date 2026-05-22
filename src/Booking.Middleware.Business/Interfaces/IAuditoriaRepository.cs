using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

public interface IAuditoriaRepository
{
    Task<(bool Success, long IdLogAuditoria, string Mensaje)> RegistrarEventoAsync(EventoDto evento, CancellationToken ct = default);
    Task<(int Exitosos, int Fallidos)> RegistrarEventosAsync(IEnumerable<EventoDto> eventos, CancellationToken ct = default);
}
