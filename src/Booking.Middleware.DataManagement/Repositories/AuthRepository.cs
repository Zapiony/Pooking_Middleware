using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;

namespace Booking.Middleware.DataManagement.Repositories;

/// <summary>
/// Repositorio sobre el cliente gRPC de Auth.
/// Permite agregar caché de resolución de usuarios sin afectar capas superiores.
/// </summary>
public sealed class AuthRepository
{
    private readonly IAuthGrpcClient _client;

    public AuthRepository(IAuthGrpcClient client)
    {
        _client = client;
    }

    public Task<UsuarioResolucionDto> ObtenerUsuarioPorGuidAsync(
        string guidUsuario,
        CancellationToken ct = default)
        => _client.ObtenerUsuarioPorGuidAsync(guidUsuario, ct);

    public Task<(bool EsValido, string Mensaje)> ValidarUsuarioActivoAsync(
        string guidUsuario,
        CancellationToken ct = default)
        => _client.ValidarUsuarioActivoAsync(guidUsuario, ct);
}
