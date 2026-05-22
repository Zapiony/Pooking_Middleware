using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

public interface IAuthRepository
{
    Task<UsuarioResolucionDto> ObtenerUsuarioPorGuidAsync(string guidUsuario, CancellationToken ct = default);
    Task<(bool EsValido, string Mensaje)> ValidarUsuarioActivoAsync(string guidUsuario, CancellationToken ct = default);
}
