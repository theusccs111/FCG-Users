
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*

# Enable the agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=07f2332bfb8dd9adf9fd72ff3eb651f3FFFFNRAL \
NEW_RELIC_APP_NAME="FIAP-Users"

WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Forçar timezone UTC
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todos os .csproj primeiro
COPY ["FCG-Users.Api/FCG-Users.Api.csproj", "FCG-Users.Api/"]
COPY ["FCG-Users.Application/FCG-Users.Application.csproj", "FCG-Users.Application/"]
COPY ["FCG-Users.Domain/FCG-Users.Domain.csproj", "FCG-Users.Domain/"]
COPY ["FCG-Users.Infrastructure/FCG-Users.Infrastructure.csproj", "FCG-Users.Infrastructure/"]

# Restaura apenas a API
RUN dotnet restore "FCG-Users.Api/FCG-Users.Api.csproj"

# Copia todo o resto
COPY . .

WORKDIR "/src/FCG-Users.Api"
RUN dotnet build "FCG-Users.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FCG-Users.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FCG-Users.Api.dll"]