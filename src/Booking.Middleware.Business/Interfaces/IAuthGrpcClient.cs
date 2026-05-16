using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Abstracción del cliente gRPC hacia el microservicio de Auth.
/// Implementada en DataAccess/GrpcClients/AuthGrpcClient.cs
/// </summary>
public interface IAuthGrpcClient
{
    Task<UsuarioResolucionDto> ObtenerUsuarioPorGuidAsync(
        string guidUsuario,
        CancellationToken ct = default);

    Task<(bool EsValido, string Mensaje)> ValidarUsuarioActivoAsync(
        string guidUsuario,
        CancellationToken ct = default);
}
