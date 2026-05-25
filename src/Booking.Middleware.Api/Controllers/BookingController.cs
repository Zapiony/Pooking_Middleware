using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Booking.Middleware.Api.DTOs;

namespace Booking.Middleware.Api.Controllers;

[ApiController]
[Route("api/v2/booking")]
public class BookingController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _authBaseUrl;
    private readonly string _clienteBaseUrl;
    private readonly string _servicioBaseUrl;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BookingController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _authBaseUrl = configuration["GrpcEndpoints:Auth"]?.TrimEnd('/') 
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Auth en configuración.");
        _clienteBaseUrl = configuration["GrpcEndpoints:Cliente"]?.TrimEnd('/') 
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Cliente en configuración.");
        _servicioBaseUrl = configuration["GrpcEndpoints:Servicio"]?.TrimEnd('/') 
            ?? throw new InvalidOperationException("Falta GrpcEndpoints:Servicio en configuración.");
        _logger = logger;
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        _logger.LogInformation("Gateway: Procesando Login en Auth Service.");
        var targetUrl = $"{_authBaseUrl}/api/v1/auth/login";
        return await ForwardRequest(targetUrl, HttpMethod.Post, loginRequest);
    }

    [HttpPost("auth/registro")]
    public async Task<IActionResult> Registro([FromBody] CrearUsuarioRequest request)
    {
        _logger.LogInformation("Gateway: Iniciando Registro Orquestado para {Email}", request.Correo);

        // ── 1. Registrar el usuario en el microservicio de Auth ──
        var authRequest = new
        {
            identificador = request.Identificador,
            correo = request.Correo,
            password = request.Password,
            nombreRol = string.IsNullOrWhiteSpace(request.NombreRol) ? "CLIENTE" : request.NombreRol,
            creadoPorUsuario = request.Correo
        };

        var authUrl = $"{_authBaseUrl}/api/v1/auth/registro";
        var authContent = new StringContent(JsonSerializer.Serialize(authRequest), Encoding.UTF8, "application/json");

        HttpResponseMessage authResponse;
        try
        {
            authResponse = await _httpClient.PostAsync(authUrl, authContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: Error de conexión con microservicio de Auth.");
            return StatusCode(503, ApiErrorResponse.Crear("El servicio de Auth no está disponible temporalmente.", 503));
        }

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        if (!authResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gateway: Registro de usuario en Auth falló con estado {Status}. Body: {Body}", authResponse.StatusCode, authResponseBody);
            return StatusCode((int)authResponse.StatusCode, JsonSerializer.Deserialize<JsonElement>(authResponseBody));
        }

        // Parsear respuesta para obtener usuarioGuid
        Guid usuarioGuid = Guid.Empty;
        JsonElement authJson = default;
        try
        {
            authJson = JsonSerializer.Deserialize<JsonElement>(authResponseBody);
            if (authJson.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("usuarioGuid", out var guidProp))
            {
                Guid.TryParse(guidProp.GetString(), out usuarioGuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: Error al deserializar la respuesta de registro de Auth.");
        }

        if (usuarioGuid == Guid.Empty)
        {
            _logger.LogError("Gateway: No se pudo obtener el usuarioGuid desde la respuesta del microservicio de Auth.");
            return StatusCode(500, ApiErrorResponse.Crear("Error interno al procesar el usuario creado.", 500));
        }

        _logger.LogInformation("Gateway: Usuario creado exitosamente con GUID: {Guid}", usuarioGuid);

        // ── 2. Registrar los datos del cliente en el microservicio de Cliente ──
        if (request.TieneClienteData)
        {
            var clienteRequest = new
            {
                usuarioGuidRef = usuarioGuid,
                nombres = request.Nombres,
                apellidos = request.Apellidos,
                razonSocial = request.RazonSocial,
                tipoIdentificacion = request.TipoIdentificacion,
                numeroIdentificacion = request.NumeroIdentificacion,
                correo = request.Correo,
                telefono = request.Telefono,
                direccion = request.Direccion
            };

            var clienteUrl = $"{_clienteBaseUrl}/api/v1/clientes";
            var clienteContent = new StringContent(JsonSerializer.Serialize(clienteRequest), Encoding.UTF8, "application/json");

            HttpResponseMessage clienteResponse;
            try
            {
                _logger.LogInformation("Gateway: Enviando perfil de cliente a {Url}", clienteUrl);
                clienteResponse = await _httpClient.PostAsync(clienteUrl, clienteContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway: Error de conexión con microservicio de Cliente.");
                return StatusCode(503, ApiErrorResponse.Crear("Usuario creado, pero no se pudo registrar la información del cliente.", 503));
            }

            var clienteResponseBody = await clienteResponse.Content.ReadAsStringAsync();
            if (!clienteResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gateway: Registro de perfil en Cliente falló con estado {Status}. Body: {Body}", clienteResponse.StatusCode, clienteResponseBody);
                return StatusCode((int)clienteResponse.StatusCode, JsonSerializer.Deserialize<JsonElement>(clienteResponseBody));
            }

            _logger.LogInformation("Gateway: Perfil de cliente creado exitosamente.");
        }

        // Retornar la respuesta original de Auth
        return Created(string.Empty, authJson);
    }

    [HttpPost("usuarios/buscar")]
    public async Task<IActionResult> ProxyUsuariosBuscar([FromBody] BusquedaRequest request)
    {
        var targetUrl = $"{_authBaseUrl}/api/v1/usuarios/buscar";
        return await ForwardRequest(targetUrl, HttpMethod.Post, request);
    }

    [HttpPost("clientes/buscar")]
    public async Task<IActionResult> ProxyClientesBuscar([FromBody] BusquedaRequest request)
    {
        var targetUrl = $"{_clienteBaseUrl}/api/v1/clientes/buscar";
        return await ForwardRequest(targetUrl, HttpMethod.Post, request);
    }

    [HttpPost("clientes/reservas")]
    public async Task<IActionResult> ProxyCrearReserva([FromBody] CrearReservaRequest request)
    {
        _logger.LogInformation("Gateway: Procesando creaciÃ³n de Reserva en Cliente Service.");
        var targetUrl = $"{_clienteBaseUrl}/api/v1/reservas";
        return await ForwardRequest(targetUrl, HttpMethod.Post, request);
    }

    [HttpPost("servicios/buscar")]
    public async Task<IActionResult> ProxyServiciosBuscar([FromBody] BusquedaRequest request)
    {
        var targetUrl = $"{_servicioBaseUrl}/api/v1/servicios/buscar";
        return await ForwardRequest(targetUrl, HttpMethod.Post, request);
    }

    [HttpPost("facturacion/buscar")]
    public async Task<IActionResult> ProxyFacturacionBuscar([FromBody] BusquedaRequest request)
    {
        // Asumiendo que facturacion está en el servicio de servicios por ahora, 
        // o si hay un microservicio de facturacion, se debe agregar.
        // Por el momento, de acuerdo al CatchAll, no hay una URL de facturacion explícita,
        // Si no existe, esto fallará adecuadamente hasta que se implemente el microservicio.
        var targetUrl = $"{_servicioBaseUrl}/api/v1/facturacion/buscar"; 
        return await ForwardRequest(targetUrl, HttpMethod.Post, request);
    }

    [HttpGet("clientes/usuario/{guid}")]
    public async Task<IActionResult> ProxyClienteUsuarioGuid(string guid)
    {
        var targetUrl = $"{_clienteBaseUrl}/api/v1/clientes/usuario/{guid}";
        return await ForwardRequest(targetUrl, HttpMethod.Get, null!);
    }

    [HttpGet("usuarios/disponibilidad/{username}")]
    public async Task<IActionResult> ProxyUsuariosDisponibilidad(string username)
    {
        var targetUrl = $"{_authBaseUrl}/api/v1/usuarios/disponibilidad/{username}";
        return await ForwardRequest(targetUrl, HttpMethod.Get, null!);
    }

    [HttpGet("clientes/disponibilidad/{tipo}/{numero_identificacion}")]
    public async Task<IActionResult> ProxyClientesDisponibilidad(string tipo, string numero_identificacion)
    {
        var targetUrl = $"{_clienteBaseUrl}/api/v1/clientes/disponibilidad/{tipo}/{numero_identificacion}";
        return await ForwardRequest(targetUrl, HttpMethod.Get, null!);
    }

    [HttpGet("clientes/disponibilidad/correo/{correo}")]
    public async Task<IActionResult> ProxyClientesDisponibilidadCorreo(string correo)
    {
        var targetUrl = $"{_clienteBaseUrl}/api/v1/clientes/disponibilidad/correo/{correo}";
        return await ForwardRequest(targetUrl, HttpMethod.Get, null!);
    }


    [Route("{*path}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ProxyCatchAll(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(ApiErrorResponse.Crear("Ruta vacía no soportada por el API Gateway.", 400));
        }

        // Determinar destino
        var segments = path.Split('/');
        var firstSegment = segments[0].ToLowerInvariant();

        string targetBaseUrl;
        if (firstSegment == "clientes" || firstSegment == "reservas" || firstSegment == "favoritos")
        {
            targetBaseUrl = _clienteBaseUrl;
        }
        else if (firstSegment == "servicios" || firstSegment == "tiposervicios")
        {
            targetBaseUrl = _servicioBaseUrl;
        }
        else if (firstSegment == "auth" || firstSegment == "usuarios" || firstSegment == "roles")
        {
            targetBaseUrl = _authBaseUrl;
        }
        else
        {
            _logger.LogWarning("Gateway: Ruta no reconocida '{Segment}' en {Path}", firstSegment, path);
            return NotFound(ApiErrorResponse.Crear($"Ruta '{path}' no enrutable por el API Gateway.", 404));
        }

        var targetUrl = $"{targetBaseUrl}/api/v1/{path}{Request.QueryString}";
        _logger.LogInformation("Gateway: Redirigiendo {Method} a {Url}", Request.Method, targetUrl);

        var method = new HttpMethod(Request.Method);
        
        // Copiar cuerpo si aplica
        HttpContent? requestContent = null;
        if (HttpMethods.IsPost(Request.Method) || HttpMethods.IsPut(Request.Method) || HttpMethods.IsPatch(Request.Method))
        {
            Request.EnableBuffering();
            var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);
            stream.Position = 0;
            requestContent = new StreamContent(stream);
            if (!string.IsNullOrEmpty(Request.ContentType))
            {
                requestContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(Request.ContentType);
            }
        }

        var requestMessage = new HttpRequestMessage(method, targetUrl)
        {
            Content = requestContent
        };

        // Copiar encabezados relevantes
        foreach (var header in Request.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsByteArrayAsync();

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            HttpContext.Response.StatusCode = (int)response.StatusCode;
            return File(responseBody, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: Error al redirigir petición a {Url}", targetUrl);
            return StatusCode(502, ApiErrorResponse.Crear("Error de comunicación con el microservicio correspondiente (Bad Gateway).", 502));
        }
    }

    private async Task<IActionResult> ForwardRequest(string targetUrl, HttpMethod method, object? body = null)
    {
        var requestMessage = new HttpRequestMessage(method, targetUrl);

        if (body != null)
        {
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            requestMessage.Content = content;
        }

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToArray());
        }

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsByteArrayAsync();

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            HttpContext.Response.StatusCode = (int)response.StatusCode;
            return File(responseBody, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: Error redirigiendo petición a {Url}", targetUrl);
            return StatusCode(502, ApiErrorResponse.Crear("Error al contactar con el servicio de destino.", 502));
        }
    }
}
