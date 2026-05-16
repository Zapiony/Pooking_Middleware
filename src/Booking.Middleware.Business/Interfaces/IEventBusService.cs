using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Contrato para la publicación asíncrona de eventos de auditoría.
/// Implementado en Business/Services/EventBusService.cs
/// Patrón: fire-and-forget con retry en backoff exponencial.
/// </summary>
public interface IEventBusService
{
    /// <summary>
    /// Publica un único evento de auditoría.
    /// </summary>
    Task<(bool Success, long IdLogAuditoria, string Mensaje)> PublicarEventoAsync(
        EventoDto evento,
        CancellationToken ct = default);

    /// <summary>
    /// Publica un lote de eventos (máx. 20) en una sola llamada gRPC.
    /// </summary>
    Task<(int Exitosos, int Fallidos)> PublicarEventosAsync(
        IEnumerable<EventoDto> eventos,
        CancellationToken ct = default);
}
