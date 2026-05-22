namespace Booking.Middleware.Api.DTOs;

public class CrearUsuarioRequest
{
    // Datos de usuario
    [System.Text.Json.Serialization.JsonPropertyName("identificador")]
    public string Identificador { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NombreRol { get; set; } = string.Empty;
    public string CreadoPorUsuario { get; set; } = string.Empty;

    // Datos de cliente
    public string? TipoIdentificacion { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? RazonSocial { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    public bool TieneClienteData =>
        !string.IsNullOrWhiteSpace(TipoIdentificacion) &&
        !string.IsNullOrWhiteSpace(NumeroIdentificacion);
}
