using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;
using Microservicio.Pooking.Servicio.Api.Protos;

namespace Booking.Middleware.DataAccess.GrpcClients;

/// <summary>
/// Cliente gRPC concreto hacia el microservicio de Servicio.
/// Usa el stub generado desde servicio.proto con GrpcServices="Client".
/// </summary>
public sealed class ServicioGrpcClient : IServicioGrpcClient
{
    private readonly ServicioGrpc.ServicioGrpcClient _client;

    public ServicioGrpcClient(ServicioGrpc.ServicioGrpcClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ServicioResolucionDto> ObtenerServicioPorGuidAsync(
        string guidServicio,
        CancellationToken ct = default)
    {
        var request = new ObtenerServicioRequest { GuidServicio = guidServicio };

        try
        {
            var reply = await _client.ObtenerServicioPorGuidAsync(request, cancellationToken: ct);

            return new ServicioResolucionDto
            {
                Found            = reply.Found,
                GuidServicio     = reply.GuidServicio,
                RazonSocial      = reply.RazonSocial,
                NombreComercial  = reply.NombreComercial,
                TipoServicio     = reply.TipoServicio,
                Estado           = reply.Estado,
                CorreoContacto   = reply.CorreoContacto,
                TelefonoContacto = reply.TelefonoContacto,
                LogoUrl          = reply.LogoUrl,
                Mensaje          = string.Empty
            };
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return new ServicioResolucionDto
            {
                Found    = false,
                Mensaje  = ex.Status.Detail
            };
        }
    }

    /// <inheritdoc />
    public async Task<DisponibilidadDto> ValidarDisponibilidadAsync(
        string guidServicio,
        string fechaInicio,
        string fechaFin,
        CancellationToken ct = default)
    {
        var request = new ValidarDisponibilidadRequest
        {
            GuidServicio = guidServicio,
            FechaInicio  = fechaInicio,
            FechaFin     = fechaFin
        };

        var reply = await _client.ValidarDisponibilidadAsync(request, cancellationToken: ct);

        return new DisponibilidadDto
        {
            Disponible = reply.Disponible,
            Motivo     = reply.Motivo
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServicioResolucionDto>> ObtenerServiciosBatchAsync(
        IEnumerable<string> guidsServicio,
        CancellationToken ct = default)
    {
        var request = new ObtenerServiciosBatchRequest();
        request.GuidsServicio.AddRange(guidsServicio);

        var reply = await _client.ObtenerServiciosBatchAsync(request, cancellationToken: ct);

        return reply.Servicios.Select(s => new ServicioResolucionDto
        {
            Found            = s.Found,
            GuidServicio     = s.GuidServicio,
            RazonSocial      = s.RazonSocial,
            NombreComercial  = s.NombreComercial,
            TipoServicio     = s.TipoServicio,
            Estado           = s.Estado,
            CorreoContacto   = s.CorreoContacto,
            TelefonoContacto = s.TelefonoContacto,
            LogoUrl          = s.LogoUrl
        }).ToList().AsReadOnly();
    }
}
