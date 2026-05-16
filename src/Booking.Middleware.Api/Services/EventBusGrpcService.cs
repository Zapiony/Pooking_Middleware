using Booking.Middleware.Api.Protos;
using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;
using Grpc.Core;

namespace Booking.Middleware.Api.Services;

/// <summary>
/// Servidor gRPC para el bus de eventos asíncrono.
/// Recibe llamadas de los microservicios productores (Cliente, Auth, Servicio)
/// y las delega a IEventBusService → AuditoriaGrpcClient → Auditoria.
/// </summary>
public sealed class EventBusGrpcHandler : EventBusGrpcService.EventBusGrpcServiceBase
{
    private readonly IEventBusService _eventBusService;
    private readonly ILogger<EventBusGrpcHandler> _logger;

    public EventBusGrpcHandler(
        IEventBusService eventBusService,
        ILogger<EventBusGrpcHandler> logger)
    {
        _eventBusService = eventBusService;
        _logger = logger;
    }

    /// <summary>
    /// Recibe un único evento y lo reenvía a Auditoria.
    /// </summary>
    public override async Task<EventoReply> PublicarEvento(
        EventoRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.IdCorrelacion))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El campo id_correlacion es obligatorio."));
        }

        try
        {
            var dto = MapearADto(request);
            var (success, idLog, mensaje) = await _eventBusService.PublicarEventoAsync(
                dto, context.CancellationToken);

            return new EventoReply
            {
                Success        = success,
                IdCorrelacion  = request.IdCorrelacion,
                IdLogAuditoria = idLog,
                Mensaje        = mensaje
            };
        }
        catch (Business.Exceptions.ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Servicio {Servicio} no disponible al publicar evento [{IdCorr}]",
                ex.ServicioAfectado, request.IdCorrelacion);

            throw new RpcException(new Status(
                StatusCode.Unavailable,
                $"El servicio {ex.ServicioAfectado} no está disponible: {ex.Message}"));
        }
        catch (Business.Exceptions.MiddlewareException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    /// <summary>
    /// Recibe un lote de eventos (máx. 20) y los reenvía en una sola llamada a Auditoria.
    /// </summary>
    public override async Task<EventosBatchReply> PublicarEventosBatch(
        EventosBatchRequest request,
        ServerCallContext context)
    {
        if (request.Eventos.Count == 0)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El lote no puede estar vacío."));
        }

        if (request.Eventos.Count > 20)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El lote no puede superar los 20 eventos."));
        }

        try
        {
            var dtos = request.Eventos.Select(MapearADto).ToList();
            var (exitosos, fallidos) = await _eventBusService.PublicarEventosAsync(
                dtos, context.CancellationToken);

            return new EventosBatchReply
            {
                Exitosos = exitosos,
                Fallidos = fallidos
            };
        }
        catch (Business.Exceptions.ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Servicio {Servicio} no disponible al publicar lote",
                ex.ServicioAfectado);

            throw new RpcException(new Status(
                StatusCode.Unavailable,
                $"El servicio {ex.ServicioAfectado} no está disponible."));
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static EventoDto MapearADto(EventoRequest r) => new()
    {
        IdCorrelacion   = r.IdCorrelacion,
        TablaAfectada   = r.TablaAfectada,
        EsquemaAfectado = r.EsquemaAfectado,
        Operacion       = (int)r.Operacion,
        IdRegistro      = r.IdRegistro,
        DatosAnteriores = r.DatosAnteriores,
        DatosNuevos     = r.DatosNuevos,
        Usuario         = r.Usuario,
        Ip              = r.Ip,
        ServicioOrigen  = r.ServicioOrigen,
        EquipoOrigen    = r.EquipoOrigen
    };
}
