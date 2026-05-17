using Booking.Middleware.Business.DTOs;

namespace Booking.Middleware.Business.Interfaces;

/// <summary>
/// Abstracción del cliente gRPC hacia el microservicio de Cliente.
/// Implementada en DataAccess/GrpcClients/ClienteGrpcClient.cs
/// </summary>
public interface IClienteGrpcClient
{
    Task<ClienteResolucionDto> ObtenerClientePorGuidAsync(
        string guidCliente,
        CancellationToken ct = default);

    Task<IReadOnlyList<ClienteResolucionDto>> ObtenerClientesBatchAsync(
        IEnumerable<string> guidsCliente,
        CancellationToken ct = default);
}
