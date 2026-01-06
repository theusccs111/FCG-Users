# Imagem base ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Imagem com SDK para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia apenas os csproj primeiro para otimizar cache
COPY FCG-Users.Api/FCG-Users.Api.csproj FCG-Users.Api/
COPY FCG-Users.Application/FCG-Users.Application.csproj FCG-Users.Application/
COPY FCG-Users.Domain/FCG-Users.Domain.csproj FCG-Users.Domain/
COPY FCG-Users.Infrastructure/FCG-Users.Infrastructure.csproj FCG-Users.Infrastructure/
COPY FCG-Users.Consumer/FCG-Users.Consumer.csproj FCG-Users.Consumer/

RUN dotnet restore FCG-Users.Api/FCG-Users.Api.csproj

# Copia tudo
COPY . .

WORKDIR "/src/FCG-Users.Api"
RUN dotnet build "FCG-Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publica
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FCG-Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FCG-Users.Api.dll"]
