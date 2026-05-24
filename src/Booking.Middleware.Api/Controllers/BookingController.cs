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
        try
        {
            Response.Headers["X-Booking-Registro-Handler"] = "BookingController.Registro";
            _logger.LogInformation("DIAGNOSTICO Registro: entrando a BookingController.Registro para POST /api/v2/booking/auth/registro.");
            _logger.LogInformation("Gateway: Iniciando Registro Orquestado para {Email}", request.Correo);

            _logger.LogInformation("Registro: antes de validar identificador.");
            _logger.LogInformation("Registro: antes de validar correo.");
            var validacionPrevia = await ValidarDisponibilidadRegistroAsync(request);
            _logger.LogInformation("Registro: después de validar identificador y correo. Resultado={TieneError}", validacionPrevia != null);
            if (validacionPrevia != null)
            {
                return validacionPrevia;
            }

            // ── 1. Registrar el usuario en el microservicio de Auth ──
            var authRequest = new
            {
                username = request.Identificador,
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
                _logger.LogInformation("Registro: antes de llamar Auth. Url={Url}", authUrl);
                authResponse = await _httpClient.PostAsync(authUrl, authContent);
                _logger.LogInformation("Registro: después de llamar Auth. Status={Status}", authResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway: Error de conexión con microservicio de Auth.");
                return StatusCode(503, ApiErrorResponse.Crear("El servicio de Auth no está disponible temporalmente.", 503));
            }

            var authResponseBody = await authResponse.Content.ReadAsStringAsync();
            var authStatusCode = (int)authResponse.StatusCode;
            _logger.LogInformation("DIAGNOSTICO Registro: AuthStatusCode={AuthStatusCode}. AuthResponseBody={AuthResponseBody}", authStatusCode, authResponseBody);
            if (!authResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gateway: Registro de usuario en Auth falló con estado {Status}. Body: {Body}", authResponse.StatusCode, authResponseBody);
                return CrearErrorServicioExterno(authResponse, authResponseBody, "Auth", "No se pudo registrar el usuario en Auth.");
            }

            // Parsear respuesta para obtener usuarioGuid
            Guid usuarioGuid = Guid.Empty;
            JsonElement authJson = default;
            try
            {
                _logger.LogInformation("Registro: antes de leer usuarioGuid desde respuesta Auth.");
                _logger.LogInformation("Registro: body exitoso de Auth: {Body}", authResponseBody);
                authJson = JsonSerializer.Deserialize<JsonElement>(authResponseBody);
                TryObtenerUsuarioGuidAuth(authJson, out usuarioGuid);
                _logger.LogInformation("Registro: después de leer usuarioGuid. UsuarioGuid={UsuarioGuid}", usuarioGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway: Error al deserializar la respuesta de registro de Auth.");
            }

            if (usuarioGuid == Guid.Empty)
            {
                _logger.LogError("Gateway: No se pudo obtener el usuarioGuid desde la respuesta del microservicio de Auth. Body: {Body}", authResponseBody);
                var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (env.IsDevelopment())
                {
                    return StatusCode(502, new
                    {
                        message = "Auth creó el usuario, pero no devolvió el identificador necesario para crear el cliente.",
                        handler = "BookingController.Registro",
                        route = "POST /api/v2/booking/auth/registro",
                        authStatusCode,
                        authResponseBody,
                        camposDetectados = ObtenerCamposJson(authResponseBody)
                    });
                }

                return StatusCode(502, ApiErrorResponse.Crear("Auth creó el usuario, pero no devolvió el identificador necesario para crear el cliente.", 502));
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
                var clienteRequestJson = JsonSerializer.Serialize(clienteRequest);
                _logger.LogInformation("DIAGNOSTICO Registro: JSON enviado a Cliente POST /api/v1/clientes: {ClienteRequestJson}", clienteRequestJson);
                var clienteContent = new StringContent(clienteRequestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage clienteResponse;
                try
                {
                    _logger.LogInformation("Registro: antes de llamar Cliente. Url={Url}", clienteUrl);
                    clienteResponse = await _httpClient.PostAsync(clienteUrl, clienteContent);
                    _logger.LogInformation("Registro: después de llamar Cliente. Status={Status}", clienteResponse.StatusCode);
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
                    return CrearErrorServicioExterno(clienteResponse, clienteResponseBody, "Cliente", "Usuario creado en Auth, pero no se pudo registrar la información del cliente.");
                }

                _logger.LogInformation("Gateway: Perfil de cliente creado exitosamente.");
            }

            // Retornar la respuesta original de Auth
            return Created(string.Empty, authJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error real en Registro");

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                return StatusCode(500, new
                {
                    exceptionType = ex.GetType().FullName,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }

            return StatusCode(500, ApiErrorResponse.Crear("Error interno del servidor.", 500));
        }
    }

    private async Task<IActionResult?> ValidarDisponibilidadRegistroAsync(CrearUsuarioRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Identificador))
        {
            var usernameUrl = $"{_authBaseUrl}/api/v1/usuarios/disponibilidad/{Uri.EscapeDataString(request.Identificador)}";
            var resultado = await ValidarDisponibilidadAsync(
                usernameUrl,
                "Auth",
                "identificador",
                $"El identificador '{request.Identificador}' ya está en uso.");

            if (resultado != null)
                return resultado;
        }

        if (!string.IsNullOrWhiteSpace(request.Correo))
        {
            var correoUrl = $"{_authBaseUrl}/api/v1/usuarios/disponibilidad-correo/{Uri.EscapeDataString(request.Correo)}";
            var resultado = await ValidarDisponibilidadAsync(
                correoUrl,
                "Auth",
                "correo",
                $"El correo '{request.Correo}' ya está en uso.");

            if (resultado != null)
                return resultado;
        }

        return null;
    }

    private static bool TryObtenerUsuarioGuidAuth(JsonElement authJson, out Guid usuarioGuid)
    {
        usuarioGuid = Guid.Empty;

        if (TryObtenerGuidPorRuta(authJson, out usuarioGuid, "data", "usuarioGuid") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "data", "usuario_guid") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "data", "id") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "data", "userId") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "data", "usuario", "id") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "usuarioGuid") ||
            TryObtenerGuidPorRuta(authJson, out usuarioGuid, "usuario_guid"))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<string> ObtenerCamposJson(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return ["<body vacío>"];

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var campos = new List<string>();
            AgregarCamposJson(document.RootElement, "$", campos);
            return campos.Count == 0 ? ["<sin campos JSON detectables>"] : campos;
        }
        catch (JsonException ex)
        {
            return [$"<JSON inválido: {ex.Message}>"];
        }
    }

    private static void AgregarCamposJson(JsonElement element, string path, List<string> campos)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = path == "$" ? property.Name : $"{path}.{property.Name}";
                campos.Add(propertyPath);
                AgregarCamposJson(property.Value, propertyPath, campos);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                AgregarCamposJson(item, $"{path}[{index}]", campos);
                index++;
            }
        }
    }

    private static bool TryObtenerGuidPorRuta(JsonElement root, out Guid guid, params string[] path)
    {
        guid = Guid.Empty;
        var current = root;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !TryGetPropertyIgnoreCase(current, segment, out current))
            {
                return false;
            }
        }

        return TryConvertirGuid(current, out guid);
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryConvertirGuid(JsonElement element, out Guid guid)
    {
        guid = Guid.Empty;

        if (element.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(element.GetString(), out guid);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyIgnoreCase(element, "value", out var valueProp) &&
                TryConvertirGuid(valueProp, out guid))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<IActionResult?> ValidarDisponibilidadAsync(
        string url,
        string servicio,
        string campo,
        string mensajeNoDisponible)
    {
        HttpResponseMessage response;
        try
        {
            _logger.LogInformation("Gateway: Validando disponibilidad de {Campo} en {Servicio}.", campo, servicio);
            response = await _httpClient.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: Error de conexión validando disponibilidad de {Campo} en {Servicio}.", campo, servicio);
            return StatusCode(503, ApiErrorResponse.Crear(
                $"No se pudo validar la disponibilidad de {campo}. Intente nuevamente.",
                503));
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Gateway: Validación de disponibilidad de {Campo} en {Servicio} falló con estado {Status}. Body: {Body}",
                campo,
                servicio,
                response.StatusCode,
                responseBody);

            return CrearErrorServicioExterno(
                response,
                responseBody,
                servicio,
                $"No se pudo validar la disponibilidad de {campo}.");
        }

        if (!TryObtenerDisponibilidad(responseBody, out var disponible))
        {
            _logger.LogWarning(
                "Gateway: Respuesta de disponibilidad de {Campo} en {Servicio} no reconocida. Body: {Body}",
                campo,
                servicio,
                responseBody);

            return StatusCode(502, ApiErrorResponse.Crear(
                $"No se pudo interpretar la validación de disponibilidad de {campo}.",
                502));
        }

        if (!disponible)
        {
            return Conflict(ApiErrorResponse.Crear(mensajeNoDisponible, 409));
        }

        return null;
    }

    private static bool TryObtenerDisponibilidad(string responseBody, out bool disponible)
    {
        disponible = false;

        if (string.IsNullOrWhiteSpace(responseBody))
            return false;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return TryObtenerDisponibilidad(document.RootElement, out disponible);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryObtenerDisponibilidad(JsonElement element, out bool disponible)
    {
        disponible = false;

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            disponible = element.GetBoolean();
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return TryObtenerDisponibilidadDesdeTexto(element.GetString(), out disponible);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    var value = property.Value.GetBoolean();
                    var name = property.Name.ToLowerInvariant();

                    if (name is "disponible" or "estadodisponible" or "isavailable" or "available" or "esdisponible" or "data" or "result" or "resultado")
                    {
                        disponible = value;
                        return true;
                    }

                    if (name is "existe" or "exists" or "existeusuario" or "existecliente" or "enuso" or "usado")
                    {
                        disponible = !value;
                        return true;
                    }
                }

                if (property.Value.ValueKind == JsonValueKind.String &&
                    TryObtenerDisponibilidadDesdeTexto(property.Value.GetString(), out disponible))
                {
                    return true;
                }

                if ((property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) &&
                    TryObtenerDisponibilidad(property.Value, out disponible))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryObtenerDisponibilidad(item, out disponible))
                    return true;
            }
        }

        return false;
    }

    private static bool TryObtenerDisponibilidadDesdeTexto(string? value, out bool disponible)
    {
        disponible = false;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var texto = value.Trim().ToLowerInvariant();
        if (bool.TryParse(texto, out disponible))
            return true;

        if (texto is "disponible" or "available" or "libre")
        {
            disponible = true;
            return true;
        }

        if (texto is "no disponible" or "unavailable" or "ocupado" or "en uso" or "existe")
        {
            disponible = false;
            return true;
        }

        return false;
    }

    private IActionResult CrearErrorServicioExterno(
        HttpResponseMessage response,
        string responseBody,
        string servicio,
        string mensajeFallback)
    {
        var statusCode = (int)response.StatusCode;

        if (TryParseJsonElement(responseBody, out var json))
        {
            return StatusCode(statusCode, json);
        }

        var errors = string.IsNullOrWhiteSpace(responseBody)
            ? Array.Empty<string>()
            : new[] { responseBody };

        _logger.LogWarning(
            "Gateway: {Servicio} devolvió un error no JSON con estado {Status}.",
            servicio,
            response.StatusCode);

        return StatusCode(statusCode, ApiErrorResponse.Crear(mensajeFallback, statusCode, errors));
    }

    private static bool TryParseJsonElement(string responseBody, out JsonElement json)
    {
        json = default;

        if (string.IsNullOrWhiteSpace(responseBody))
            return false;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            json = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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

    [HttpGet("usuarios/disponibilidad-correo/{correo}")]
    public async Task<IActionResult> ProxyUsuariosDisponibilidadCorreo(string correo)
    {
        var targetUrl = $"{_authBaseUrl}/api/v1/usuarios/disponibilidad-correo/{correo}";
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
