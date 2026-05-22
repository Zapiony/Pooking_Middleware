using Booking.Middleware.Business.DTOs;
using Booking.Middleware.Business.Interfaces;

namespace Booking.Middleware.DataManagement.Repositories;

/// <summary>
/// Repositorio sobre el cliente gRPC de Cliente.
/// Permite agregar caché u otras lógicas sin afectar la capa de negocio.
/// </summary>
public sealed class ClienteRepository : IClienteRepository
{
    private readonly IClienteGrpcClient _client;

    public ClienteRepository(IClienteGrpcClient client)
    {
        _client = client;
    }

    public Task<ClienteResolucionDto> ObtenerClientePorGuidAsync(
        string guidCliente,
        CancellationToken ct = default)
        => _client.ObtenerClientePorGuidAsync(guidCliente, ct);

    public Task<IReadOnlyList<ClienteResolucionDto>> ObtenerClientesBatchAsync(
        IEnumerable<string> guidsCliente,
        CancellationToken ct = default)
        => _client.ObtenerClientesBatchAsync(guidsCliente, ct);
}
