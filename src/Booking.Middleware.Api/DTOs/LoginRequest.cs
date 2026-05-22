using System.Text.Json.Serialization;

namespace Booking.Middleware.Api.DTOs;

public class LoginRequest
{
    [JsonPropertyName("identificador")]
    public string Identificador { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
