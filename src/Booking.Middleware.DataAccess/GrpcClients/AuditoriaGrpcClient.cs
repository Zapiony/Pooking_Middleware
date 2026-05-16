using Booking.Auditoria.API.Protos;
using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;

namespace Booking.Middleware.DataAccess.GrpcClients;

/// <summary>
/// Cliente gRPC concreto hacia el microservicio de Auditoria.
/// Usa el stub generado desde audit.proto con GrpcServices="Client".
/// </summary>
public sealed class AuditoriaGrpcClient : IAuditoriaGrpcClient
{
    private readonly AuditoriaGrpcService.AuditoriaGrpcServiceClient _client;

    public AuditoriaGrpcClient(AuditoriaGrpcService.AuditoriaGrpcServiceClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<(bool Success, long IdLogAuditoria, string Mensaje)> RegistrarEventoAsync(
        EventoDto evento,
        CancellationToken ct = default)
    {
        var request = new AuditoriaRequest
        {
            IdCorrelacion   = evento.IdCorrelacion,
            TablaAfectada   = evento.TablaAfectada,
            EsquemaAfectado = evento.EsquemaAfectado,
            Operacion       = evento.Operacion,
            IdRegistro      = evento.IdRegistro,
            DatosAnteriores = evento.DatosAnteriores,
            DatosNuevos     = evento.DatosNuevos,
            Usuario         = evento.Usuario,
            Ip              = evento.Ip,
            ServicioOrigen  = evento.ServicioOrigen,
            EquipoOrigen    = evento.EquipoOrigen
        };

        var reply = await _client.RegistrarEventoAsync(request, cancellationToken: ct);
        return (reply.Success, reply.IdLogAuditoria, reply.Mensaje);
    }

    /// <inheritdoc />
    public async Task<(int Exitosos, int Fallidos)> RegistrarEventosAsync(
        IEnumerable<EventoDto> eventos,
        CancellationToken ct = default)
    {
        var batchRequest = new AuditoriaListRequest();
        batchRequest.Eventos.AddRange(eventos.Select(e => new AuditoriaRequest
        {
            IdCorrelacion   = e.IdCorrelacion,
            TablaAfectada   = e.TablaAfectada,
            EsquemaAfectado = e.EsquemaAfectado,
            Operacion       = e.Operacion,
            IdRegistro      = e.IdRegistro,
            DatosAnteriores = e.DatosAnteriores,
            DatosNuevos     = e.DatosNuevos,
            Usuario         = e.Usuario,
            Ip              = e.Ip,
            ServicioOrigen  = e.ServicioOrigen,
            EquipoOrigen    = e.EquipoOrigen
        }));

        var reply = await _client.RegistrarEventosAsync(batchRequest, cancellationToken: ct);
        return (reply.Exitosos, reply.Fallidos);
    }
}
