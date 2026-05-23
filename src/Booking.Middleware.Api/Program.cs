using Booking.Middleware.Api.Extensions;
using Booking.Middleware.Api.Middleware;

// ── HTTP/2 sin TLS en desarrollo local ───────────────────────────────────────
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// ── gRPC Server (este proceso como servidor) ──────────────────────────────────
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ── gRPC Clients + Interfaces de DataAccess ───────────────────────────────────
builder.Services.AddMiddlewareGrpcClients(builder.Configuration);

// ── DataManagement + Business ─────────────────────────────────────────────────
builder.Services.AddMiddlewareServices();

// ── Controllers + HttpClient + CORS + Swagger ───────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddCustomSwagger();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── YARP Reverse Proxy ────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseCors("AllowAll");

// Habilitar Swagger para facilitar pruebas de la API Gateway
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "Pooking Service Bus & API Gateway v2");
    
    // Consolidamos todos los microservicios en un único Swagger dinámico
    // NOTA: la ruta está bajo /api-docs/ para evitar que UseSwagger() la intercepte
    options.SwaggerEndpoint("/api-docs/renta-carros/swagger.json", "Renta de Carros");
    options.SwaggerEndpoint("/api-docs/atracciones/swagger.json", "Atracciones");
    options.SwaggerEndpoint("/api-docs/vuelos/swagger.json", "Vuelos");
    options.SwaggerEndpoint("/api-docs/hospedaje/swagger.json", "Hospedaje");

    options.RoutePrefix = "swagger";
});

// ── Endpoints gRPC Server ─────────────────────────────────────────────────────
app.MapMiddlewareGrpcServices();

// ── Endpoints HTTP ────────────────────────────────────────────────────────────
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    servicio = "Booking.Middleware",
    version  = "1.0",
    estado   = "activo"
}));

// ── Endpoints Swagger Consolidado ──────────────────────────────────────────────
// Ruta bajo /api-docs/ para que UseSwagger() NO la intercepte (solo conoce /swagger/v2/swagger.json)
app.MapGet("/api-docs/renta-carros/swagger.json", async (IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "base_swagger.json");
    if (!File.Exists(filePath))
        return Results.NotFound("No se encontró el archivo base_swagger.json");

    var jsonString = await File.ReadAllTextAsync(filePath);
    using var swaggerDoc = System.Text.Json.JsonDocument.Parse(jsonString);
    var root = swaggerDoc.RootElement;

    var newPaths = new System.Text.Json.Nodes.JsonObject();
    
    var developers = new Dictionary<string, string>
    {
        { "martin", "Martín" },
        { "dylan",  "Dylan" },
        { "ana",    "Ana" },
        { "kath",   "Katherin" }
    };

    if (root.TryGetProperty("paths", out var paths))
    {
        foreach (var path in paths.EnumerateObject())
        {
            var originalPath = path.Name;

            foreach (var dev in developers)
            {
                var newPath = $"/{dev.Key}{originalPath}";
                var methodsNode = System.Text.Json.Nodes.JsonNode.Parse(path.Value.GetRawText())!.AsObject();

                foreach (var method in methodsNode)
                {
                    if (method.Value is System.Text.Json.Nodes.JsonObject methodObj)
                    {
                        // Agrupamos en Swagger usando Tags con el nombre del desarrollador
                        methodObj["tags"] = new System.Text.Json.Nodes.JsonArray(dev.Value);
                    }
                }
                newPaths.Add(newPath, methodsNode);
            }
        }
    }

    var newSwaggerDoc = new System.Text.Json.Nodes.JsonObject();
    foreach (var prop in root.EnumerateObject())
    {
        if (prop.Name == "paths")
            newSwaggerDoc.Add("paths", newPaths);
        else
            newSwaggerDoc.Add(prop.Name, System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText()));
    }

    if (newSwaggerDoc.TryGetPropertyValue("info", out var infoNode) && infoNode is System.Text.Json.Nodes.JsonObject infoObj)
    {
        infoObj["title"] = "Renta de Carros - API Consolidada";
    }

    return Results.Text(newSwaggerDoc.ToJsonString(), "application/json");
});

app.MapGet("/api-docs/atracciones/swagger.json", async (IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "base_swagger_atracciones.json");
    if (!File.Exists(filePath))
        return Results.NotFound("No se encontró el archivo base_swagger_atracciones.json");

    var jsonString = await File.ReadAllTextAsync(filePath);
    using var swaggerDoc = System.Text.Json.JsonDocument.Parse(jsonString);
    var root = swaggerDoc.RootElement;

    var newPaths = new System.Text.Json.Nodes.JsonObject();
    
    var developers = new Dictionary<string, string>
    {
        { "luis", "Luis Alobuela" },
        { "jhonatan", "Jhonatan Heredia" },
        { "francisco", "Francisco Miguez" },
        { "angel", "Ángel Fonseca" }
    };

    if (root.TryGetProperty("paths", out var paths))
    {
        foreach (var path in paths.EnumerateObject())
        {
            var originalPath = path.Name;

            foreach (var dev in developers)
            {
                var newPath = $"/{dev.Key}{originalPath}";
                var methodsNode = System.Text.Json.Nodes.JsonNode.Parse(path.Value.GetRawText())!.AsObject();

                foreach (var method in methodsNode)
                {
                    if (method.Value is System.Text.Json.Nodes.JsonObject methodObj)
                    {
                        // Agrupamos en Swagger usando Tags con el nombre del desarrollador
                        methodObj["tags"] = new System.Text.Json.Nodes.JsonArray(dev.Value);
                    }
                }
                newPaths.Add(newPath, methodsNode);
            }
        }
    }

    var newSwaggerDoc = new System.Text.Json.Nodes.JsonObject();
    foreach (var prop in root.EnumerateObject())
    {
        if (prop.Name == "paths")
            newSwaggerDoc.Add("paths", newPaths);
        else
            newSwaggerDoc.Add(prop.Name, System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText()));
    }

    if (newSwaggerDoc.TryGetPropertyValue("info", out var infoNode) && infoNode is System.Text.Json.Nodes.JsonObject infoObj)
    {
        infoObj["title"] = "Atracciones - API Consolidada";
    }

    return Results.Text(newSwaggerDoc.ToJsonString(), "application/json");
});

app.MapGet("/api-docs/vuelos/swagger.json", async (IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "base_swagger_vuelos.json");
    if (!File.Exists(filePath))
        return Results.NotFound("No se encontró el archivo base_swagger_vuelos.json");

    var jsonString = await File.ReadAllTextAsync(filePath);
    using var swaggerDoc = System.Text.Json.JsonDocument.Parse(jsonString);
    var root = swaggerDoc.RootElement;

    var newPaths = new System.Text.Json.Nodes.JsonObject();
    
    var developers = new Dictionary<string, string>
    {
        { "nacho", "Nacho" },
        { "mary", "Mary" },
        { "marcillo", "Marcillo" }
    };

    if (root.TryGetProperty("paths", out var paths))
    {
        foreach (var path in paths.EnumerateObject())
        {
            var originalPath = path.Name;

            foreach (var dev in developers)
            {
                var newPath = $"/{dev.Key}{originalPath}";
                var methodsNode = System.Text.Json.Nodes.JsonNode.Parse(path.Value.GetRawText())!.AsObject();

                foreach (var method in methodsNode)
                {
                    if (method.Value is System.Text.Json.Nodes.JsonObject methodObj)
                    {
                        // Agrupamos en Swagger usando Tags con el nombre del desarrollador
                        methodObj["tags"] = new System.Text.Json.Nodes.JsonArray(dev.Value);
                    }
                }
                newPaths.Add(newPath, methodsNode);
            }
        }
    }

    var newSwaggerDoc = new System.Text.Json.Nodes.JsonObject();
    foreach (var prop in root.EnumerateObject())
    {
        if (prop.Name == "paths")
            newSwaggerDoc.Add("paths", newPaths);
        else
            newSwaggerDoc.Add(prop.Name, System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText()));
    }

    if (newSwaggerDoc.TryGetPropertyValue("info", out var infoNode) && infoNode is System.Text.Json.Nodes.JsonObject infoObj)
    {
        infoObj["title"] = "Vuelos - API Consolidada";
    }

    return Results.Text(newSwaggerDoc.ToJsonString(), "application/json");
});

app.MapGet("/api-docs/hospedaje/swagger.json", async (IWebHostEnvironment env) =>
{
    var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "base_swagger_hospedaje.json");
    if (!File.Exists(filePath))
        return Results.NotFound("No se encontró el archivo base_swagger_hospedaje.json");

    var jsonString = await File.ReadAllTextAsync(filePath);
    using var swaggerDoc = System.Text.Json.JsonDocument.Parse(jsonString);
    var root = swaggerDoc.RootElement;

    var newPaths = new System.Text.Json.Nodes.JsonObject();
    
    var developers = new Dictionary<string, string>
    {
        { "jorge", "Jorge" },
        { "jose", "Jose" },
        { "kelvin", "Kelvin" },
        { "juan", "Juan" },
        { "mateo", "Mateo" }
    };

    if (root.TryGetProperty("paths", out var paths))
    {
        foreach (var path in paths.EnumerateObject())
        {
            var originalPath = path.Name;

            foreach (var dev in developers)
            {
                var newPath = $"/{dev.Key}{originalPath}";
                var methodsNode = System.Text.Json.Nodes.JsonNode.Parse(path.Value.GetRawText())!.AsObject();

                foreach (var method in methodsNode)
                {
                    if (method.Value is System.Text.Json.Nodes.JsonObject methodObj)
                    {
                        // Agrupamos en Swagger usando Tags con el nombre del desarrollador
                        methodObj["tags"] = new System.Text.Json.Nodes.JsonArray(dev.Value);
                    }
                }
                newPaths.Add(newPath, methodsNode);
            }
        }
    }

    var newSwaggerDoc = new System.Text.Json.Nodes.JsonObject();
    foreach (var prop in root.EnumerateObject())
    {
        if (prop.Name == "paths")
            newSwaggerDoc.Add("paths", newPaths);
        else
            newSwaggerDoc.Add(prop.Name, System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText()));
    }

    if (newSwaggerDoc.TryGetPropertyValue("info", out var infoNode) && infoNode is System.Text.Json.Nodes.JsonObject infoObj)
    {
        infoObj["title"] = "Hospedaje - API Consolidada";
    }

    return Results.Text(newSwaggerDoc.ToJsonString(), "application/json");
});

// ── YARP Map Reverse Proxy ────────────────────────────────────────────────────
app.MapReverseProxy();

app.Run();
