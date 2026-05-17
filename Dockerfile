FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Forzar a .NET 10 a escuchar en el puerto 5500
ENV ASPNETCORE_URLS=http://+:5500
EXPOSE 5500

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copiar archivos de proyecto usando la estructura real
COPY ["src/Booking.Middleware.Api/Booking.Middleware.Api.csproj", "src/Booking.Middleware.Api/"]
COPY ["src/Booking.Middleware.Business/Booking.Middleware.Business.csproj", "src/Booking.Middleware.Business/"]
COPY ["src/Booking.Middleware.DataManagement/Booking.Middleware.DataManagement.csproj", "src/Booking.Middleware.DataManagement/"]
COPY ["src/Booking.Middleware.DataAccess/Booking.Middleware.DataAccess.csproj", "src/Booking.Middleware.DataAccess/"]

RUN dotnet restore "src/Booking.Middleware.Api/Booking.Middleware.Api.csproj"

# Copiar el resto del código (el .dockerignore filtrará bin/ y obj/)
COPY . .

# Compilar desde la ruta correcta sin redundancias de carpetas
WORKDIR "/source/src/Booking.Middleware.Api"
RUN dotnet build "Booking.Middleware.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Middleware.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Middleware.Api.dll"]