namespace Booking.Middleware.Api.DTOs;

public class UsuarioResponse
{
    public Guid UsuarioGuid { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string EstadoUsuario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
