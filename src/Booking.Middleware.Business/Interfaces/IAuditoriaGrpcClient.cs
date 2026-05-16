using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Abstracción del cliente gRPC hacia el servicio de Auditoria.
/// Implementada en DataAccess/GrpcClients/AuditoriaGrpcClient.cs
/// </summary>
public interface IAuditoriaGrpcClient
{
    Task<(bool Success, long IdLogAuditoria, string Mensaje)> RegistrarEventoAsync(
        EventoDto evento,
        CancellationToken ct = default);

    Task<(int Exitosos, int Fallidos)> RegistrarEventosAsync(
        IEnumerable<EventoDto> eventos,
        CancellationToken ct = default);
}
