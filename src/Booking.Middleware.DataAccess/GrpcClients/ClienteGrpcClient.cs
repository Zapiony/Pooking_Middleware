using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;
using Microservicio.Pooking.Cliente.Api.Protos;

namespace Booking.Middleware.DataAccess.GrpcClients;

/// <summary>
/// Cliente gRPC concreto hacia el microservicio de Cliente.
/// Usa el stub generado desde cliente.proto con GrpcServices="Client".
/// </summary>
public sealed class ClienteGrpcClient : IClienteGrpcClient
{
    private readonly ClienteGrpc.ClienteGrpcClient _client;

    public ClienteGrpcClient(ClienteGrpc.ClienteGrpcClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ClienteResolucionDto> ObtenerClientePorGuidAsync(
        string guidCliente,
        CancellationToken ct = default)
    {
        var request = new ObtenerClienteRequest { GuidCliente = guidCliente };

        try
        {
            var reply = await _client.ObtenerClientePorGuidAsync(request, cancellationToken: ct);

            return new ClienteResolucionDto
            {
                Found        = reply.Found,
                GuidCliente  = reply.GuidCliente,
                Nombre       = reply.Nombre,
                Apellido     = reply.Apellido,
                Correo       = reply.Correo,
                Telefono     = reply.Telefono,
                Estado       = reply.Estado,
                Mensaje      = string.Empty
            };
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return new ClienteResolucionDto
            {
                Found   = false,
                Mensaje = ex.Status.Detail
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClienteResolucionDto>> ObtenerClientesBatchAsync(
        IEnumerable<string> guidsCliente,
        CancellationToken ct = default)
    {
        var request = new ObtenerClientesBatchRequest();
        request.GuidsCliente.AddRange(guidsCliente);

        var reply = await _client.ObtenerClientesBatchAsync(request, cancellationToken: ct);

        return reply.Clientes.Select(c => new ClienteResolucionDto
        {
            Found       = c.Found,
            GuidCliente = c.GuidCliente,
            Nombre      = c.Nombre,
            Apellido    = c.Apellido,
            Correo      = c.Correo,
            Telefono    = c.Telefono,
            Estado      = c.Estado
        }).ToList().AsReadOnly();
    }
}
