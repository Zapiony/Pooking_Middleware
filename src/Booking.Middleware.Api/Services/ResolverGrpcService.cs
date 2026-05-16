using Booking.Middleware.Api.Protos;
using Booking.Middleware.Business.Interfaces;
using Grpc.Core;

namespace Booking.Middleware.Api.Services;

/// <summary>
/// Servidor gRPC para la resolución síncrona cross-dominio.
/// Los microservicios consumidores (p.ej. Cliente al crear una reserva)
/// llaman aquí para obtener datos de otro dominio sin acoplarse directamente.
/// </summary>
public sealed class ResolverGrpcHandler : ResolverGrpcService.ResolverGrpcServiceBase
{
    private readonly ICrossDomainResolverService _resolver;
    private readonly ILogger<ResolverGrpcHandler> _logger;

    public ResolverGrpcHandler(
        ICrossDomainResolverService resolver,
        ILogger<ResolverGrpcHandler> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>
    /// Resuelve el snapshot de un Servicio por su GUID.
    /// Usado en ReservasService.CrearAsync antes de persistir la reserva.
    /// </summary>
    public override async Task<ResolverServicioReply> ResolverServicio(
        ResolverServicioRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.GuidServicio))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El campo guid_servicio es obligatorio."));
        }

        try
        {
            var dto = await _resolver.ResolverServicioAsync(
                request.GuidServicio, context.CancellationToken);

            return new ResolverServicioReply
            {
                Found           = dto.Found,
                GuidServicio    = dto.GuidServicio,
                RazonSocial     = dto.RazonSocial,
                NombreComercial = dto.NombreComercial,
                TipoServicio    = dto.TipoServicio,
                Estado          = dto.Estado,
                CorreoContacto  = dto.CorreoContacto,
                TelefonoContacto = dto.TelefonoContacto,
                LogoUrl         = dto.LogoUrl,
                Mensaje         = dto.Mensaje
            };
        }
        catch (Business.Exceptions.ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Servicio {Servicio} no disponible al resolver guid [{Guid}]",
                ex.ServicioAfectado, request.GuidServicio);

            throw new RpcException(new Status(
                StatusCode.Unavailable,
                $"El servicio {ex.ServicioAfectado} no está disponible."));
        }
        catch (Business.Exceptions.MiddlewareException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    /// <summary>
    /// Resuelve los datos de un Usuario por su GUID desde el servicio Auth.
    /// </summary>
    public override async Task<ResolverUsuarioReply> ResolverUsuario(
        ResolverUsuarioRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.GuidUsuario))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El campo guid_usuario es obligatorio."));
        }

        try
        {
            var dto = await _resolver.ResolverUsuarioAsync(
                request.GuidUsuario, context.CancellationToken);

            var reply = new ResolverUsuarioReply
            {
                Found       = dto.Found,
                GuidUsuario = dto.GuidUsuario,
                Username    = dto.Username,
                Correo      = dto.Correo,
                Estado      = dto.Estado,
                Mensaje     = dto.Mensaje
            };
            reply.Roles.AddRange(dto.Roles);
            return reply;
        }
        catch (Business.Exceptions.ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Servicio Auth no disponible al resolver usuario [{Guid}]",
                request.GuidUsuario);

            throw new RpcException(new Status(
                StatusCode.Unavailable,
                "El servicio Auth no está disponible."));
        }
        catch (Business.Exceptions.MiddlewareException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    /// <summary>
    /// Valida si un Servicio tiene disponibilidad en el rango de fechas indicado.
    /// </summary>
    public override async Task<DisponibilidadReply> ValidarDisponibilidadServicio(
        ValidarDisponibilidadRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.GuidServicio))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "El campo guid_servicio es obligatorio."));
        }

        try
        {
            var dto = await _resolver.ValidarDisponibilidadServicioAsync(
                request.GuidServicio,
                request.FechaInicio,
                request.FechaFin,
                context.CancellationToken);

            return new DisponibilidadReply
            {
                Disponible = dto.Disponible,
                Motivo     = dto.Motivo
            };
        }
        catch (Business.Exceptions.ServiceUnavailableException ex)
        {
            _logger.LogError(ex,
                "Servicio {Servicio} no disponible al validar disponibilidad de [{Guid}]",
                ex.ServicioAfectado, request.GuidServicio);

            throw new RpcException(new Status(
                StatusCode.Unavailable,
                $"El servicio {ex.ServicioAfectado} no está disponible."));
        }
        catch (Business.Exceptions.MiddlewareException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
}
