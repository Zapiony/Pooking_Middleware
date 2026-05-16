using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;

namespace Booking.Middleware.DataManagement.Repositories;

/// <summary>
/// Repositorio sobre el cliente gRPC de Auditoria.
/// Capa de indirección que permite agregar caché, logging extra o transformaciones
/// sin modificar el cliente gRPC ni el servicio de negocio.
/// </summary>
public sealed class AuditoriaRepository
{
    private readonly IAuditoriaGrpcClient _client;

    public AuditoriaRepository(IAuditoriaGrpcClient client)
    {
        _client = client;
    }

    public Task<(bool Success, long IdLogAuditoria, string Mensaje)> RegistrarEventoAsync(
        EventoDto evento,
        CancellationToken ct = default)
        => _client.RegistrarEventoAsync(evento, ct);

    public Task<(int Exitosos, int Fallidos)> RegistrarEventosAsync(
        IEnumerable<EventoDto> eventos,
        CancellationToken ct = default)
        => _client.RegistrarEventosAsync(eventos, ct);
}
