using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;
using Microservicio.Pooking.Auth.Api.Protos;

namespace Booking.Middleware.DataAccess.GrpcClients;

/// <summary>
/// Cliente gRPC concreto hacia el microservicio de Auth.
/// Usa el stub generado desde auth.proto con GrpcServices="Client".
/// </summary>
public sealed class AuthGrpcClient : IAuthGrpcClient
{
    private readonly AuthGrpc.AuthGrpcClient _client;

    public AuthGrpcClient(AuthGrpc.AuthGrpcClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<UsuarioResolucionDto> ObtenerUsuarioPorGuidAsync(
        string guidUsuario,
        CancellationToken ct = default)
    {
        var request = new UsuarioGuidRequest { GuidUsuario = guidUsuario };

        try
        {
            var reply = await _client.ObtenerUsuarioPorGuidAsync(request, cancellationToken: ct);

            return new UsuarioResolucionDto
            {
                Found       = true,
                GuidUsuario = reply.GuidUsuario,
                Username    = reply.Username,
                Correo      = reply.Correo,
                Estado      = reply.EstadoUsuario,
                Roles       = [.. reply.Roles],
                Mensaje     = string.Empty
            };
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return new UsuarioResolucionDto
            {
                Found   = false,
                Mensaje = ex.Status.Detail
            };
        }
    }

    /// <inheritdoc />
    public async Task<(bool EsValido, string Mensaje)> ValidarUsuarioActivoAsync(
        string guidUsuario,
        CancellationToken ct = default)
    {
        var request = new UsuarioGuidRequest { GuidUsuario = guidUsuario };
        var reply = await _client.ValidarUsuarioActivoAsync(request, cancellationToken: ct);
        return (reply.EsValido, reply.Mensaje);
    }
}
