namespace Booking.Middleware.Business.DTOs;

/// <summary>
/// Snapshot del usuario resuelto para enrichment de una reserva.
/// Retornado por CrossDomainResolverService.ResolverUsuarioAsync().
/// </summary>
public sealed class UsuarioResolucionDto
{
    public bool     Found       { get; init; }
    public string   GuidUsuario { get; init; } = string.Empty;
    public string   Username    { get; init; } = string.Empty;
    public string   Correo      { get; init; } = string.Empty;
    public string   Estado      { get; init; } = string.Empty;
    public string[] Roles       { get; init; } = [];
    public string   Mensaje     { get; init; } = string.Empty;
}
