using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Exceptions;
using Booking.Middleware.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace Booking.Middleware.Business.Services;

/// <summary>
/// Implementación de la resolución síncrona cross-dominio.
/// Actúa como orquestador: recibe el tipo de resolución y delega al repositorio correcto.
/// Política de resiliencia: circuit-breaker agresivo (bloquea la operación del caller).
/// </summary>
public sealed class CrossDomainResolverService : ICrossDomainResolverService
{
    private readonly IServicioRepository _servicioRepo;
    private readonly IAuthRepository _authRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly ILogger<CrossDomainResolverService> _logger;

    public CrossDomainResolverService(
        IServicioRepository servicioRepo,
        IAuthRepository authRepo,
        IClienteRepository clienteRepo,
        ILogger<CrossDomainResolverService> logger)
    {
        _servicioRepo = servicioRepo;
        _authRepo = authRepo;
        _clienteRepo = clienteRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServicioResolucionDto> ResolverServicioAsync(
        string guidServicio,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guidServicio))
            throw new MiddlewareException("El guidServicio es obligatorio para la resolución.");

        try
        {
            _logger.LogInformation("Resolviendo servicio [{Guid}]", guidServicio);
            var resultado = await _servicioRepo.ObtenerServicioPorGuidAsync(guidServicio, ct);

            if (!resultado.Found)
            {
                _logger.LogWarning("Servicio [{Guid}] no encontrado en catálogo.", guidServicio);
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex, "Error al resolver servicio [{Guid}]", guidServicio);
            throw new ServiceUnavailableException(
                "Servicio",
                $"No se pudo resolver el servicio {guidServicio}: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<UsuarioResolucionDto> ResolverUsuarioAsync(
        string guidUsuario,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guidUsuario))
            throw new MiddlewareException("El guidUsuario es obligatorio para la resolución.");

        try
        {
            _logger.LogInformation("Resolviendo usuario [{Guid}]", guidUsuario);
            var resultado = await _authRepo.ObtenerUsuarioPorGuidAsync(guidUsuario, ct);

            if (!resultado.Found)
            {
                _logger.LogWarning("Usuario [{Guid}] no encontrado en Auth.", guidUsuario);
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex, "Error al resolver usuario [{Guid}]", guidUsuario);
            throw new ServiceUnavailableException(
                "Auth",
                $"No se pudo resolver el usuario {guidUsuario}: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<ClienteResolucionDto> ResolverClienteAsync(
        string guidCliente,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guidCliente))
            throw new MiddlewareException("El guidCliente es obligatorio para la resolución.");

        try
        {
            _logger.LogInformation("Resolviendo cliente [{Guid}]", guidCliente);
            var resultado = await _clienteRepo.ObtenerClientePorGuidAsync(guidCliente, ct);

            if (!resultado.Found)
            {
                _logger.LogWarning("Cliente [{Guid}] no encontrado.", guidCliente);
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex, "Error al resolver cliente [{Guid}]", guidCliente);
            throw new ServiceUnavailableException(
                "Cliente",
                $"No se pudo resolver el cliente {guidCliente}: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<DisponibilidadDto> ValidarDisponibilidadServicioAsync(
        string guidServicio,
        string fechaInicio,
        string fechaFin,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guidServicio))
            throw new MiddlewareException("El guidServicio es obligatorio para validar disponibilidad.");

        try
        {
            _logger.LogInformation(
                "Validando disponibilidad de servicio [{Guid}] de [{Inicio}] a [{Fin}]",
                guidServicio, fechaInicio, fechaFin);

            return await _servicioRepo.ValidarDisponibilidadAsync(guidServicio, fechaInicio, fechaFin, ct);
        }
        catch (Exception ex) when (ex is not MiddlewareException)
        {
            _logger.LogError(ex,
                "Error al validar disponibilidad de servicio [{Guid}]", guidServicio);
            throw new ServiceUnavailableException(
                "Servicio",
                $"No se pudo validar la disponibilidad del servicio {guidServicio}: {ex.Message}",
                ex);
        }
    }
}
