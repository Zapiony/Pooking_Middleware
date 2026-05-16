using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Contrato para la resolución síncrona de datos cross-dominio.
/// Implementado en Business/Services/CrossDomainResolverService.cs
/// Patrón: circuit-breaker agresivo porque bloquea la operación del cliente.
/// </summary>
public interface ICrossDomainResolverService
{
    /// <summary>
    /// Obtiene el snapshot de un servicio por su GUID.
    /// Usado en ReservasService antes de persistir una reserva.
    /// </summary>
    Task<ServicioResolucionDto> ResolverServicioAsync(
        string guidServicio,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene datos de un usuario por su GUID.
    /// </summary>
    Task<UsuarioResolucionDto> ResolverUsuarioAsync(
        string guidUsuario,
        CancellationToken ct = default);

    /// <summary>
    /// Valida si un servicio está disponible en el rango de fechas dado.
    /// </summary>
    Task<DisponibilidadDto> ValidarDisponibilidadServicioAsync(
        string guidServicio,
        string fechaInicio,
        string fechaFin,
        CancellationToken ct = default);
}
