using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Exceptions;
using Booking.Middleware.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace Booking.Middleware.Business.Services;

/// <summary>
/// Implementación del bus de eventos asíncrono.
/// Recibe un EventoDto del servidor gRPC y lo reenvía al repositorio de Auditoria.
/// Política de resiliencia: retry con backoff exponencial (bajo impacto si falla).
/// </summary>
public sealed class EventBusService : IEventBusService
{
    private readonly IAuditoriaGrpcClient _auditoriaClient;
    private readonly ILogger<EventBusService> _logger;

    public EventBusService(
        IAuditoriaGrpcClient auditoriaClient,
        ILogger<EventBusService> logger)
    {
        _auditoriaClient = auditoriaClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool Success, long IdLogAuditoria, string Mensaje)> PublicarEventoAsync(
        EventoDto evento,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(evento.IdCorrelacion))
            throw new MiddlewareException("El campo IdCorrelacion es obligatorio en un EventoDto.");

        try
        {
            _logger.LogInformation(
                "Publicando evento [{Operacion}] en tabla [{Tabla}] con correlación [{IdCorr}]",
                evento.Operacion, evento.TablaAfectada, evento.IdCorrelacion);

            var resultado = await _auditoriaClient.RegistrarEventoAsync(evento, ct);

            if (resultado.Success)
            {
                _logger.LogInformation(
                    "Evento [{IdCorr}] registrado correctamente. IdLog={IdLog}",
                    evento.IdCorrelacion, resultado.IdLogAuditoria);
            }
            else
            {
                _logger.LogWarning(
                    "Auditoria rechazó el evento [{IdCorr}]: {Mensaje}",
                    evento.IdCorrelacion, resultado.Mensaje);
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex,
                "Error al publicar evento [{IdCorr}] hacia Auditoria",
                evento.IdCorrelacion);

            throw new ServiceUnavailableException(
                "Auditoria",
                $"No se pudo registrar el evento de auditoría: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<(int Exitosos, int Fallidos)> PublicarEventosAsync(
        IEnumerable<EventoDto> eventos,
        CancellationToken ct = default)
    {
        var lista = eventos.ToList();

        if (lista.Count == 0)
            return (0, 0);

        if (lista.Count > 20)
            throw new MiddlewareException("No se pueden publicar más de 20 eventos en un lote.");

        try
        {
            _logger.LogInformation(
                "Publicando lote de {Count} eventos hacia Auditoria",
                lista.Count);

            var resultado = await _auditoriaClient.RegistrarEventosAsync(lista, ct);

            _logger.LogInformation(
                "Lote procesado. Exitosos={Exitosos}, Fallidos={Fallidos}",
                resultado.Exitosos, resultado.Fallidos);

            return resultado;
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex, "Error al publicar lote de eventos hacia Auditoria");

            throw new ServiceUnavailableException(
                "Auditoria",
                $"No se pudo registrar el lote de eventos: {ex.Message}",
                ex);
        }
    }
}
