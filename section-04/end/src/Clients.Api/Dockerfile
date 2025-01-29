FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Clients.Api/Clients.Api.csproj", "src/Clients.Api/"]
RUN dotnet restore "src/Clients.Api/Clients.Api.csproj"
COPY . .
WORKDIR "/src/src/Clients.Api"
RUN dotnet build "Clients.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Clients.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Install Auto Insturmentation
ARG OTEL_VERSION=1.7.0
RUN apt-get update && apt-get install -y unzip curl
ADD https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/v${OTEL_VERSION}/otel-dotnet-auto-install.sh otel-dotnet-auto-install.sh
RUN apt-get update && apt-get install -y unzip && \
    OTEL_DOTNET_AUTO_HOME="/otel-dotnet-auto" sh otel-dotnet-auto-install.sh
RUN chmod +x /otel-dotnet-auto/instrument.sh

FROM base AS final

WORKDIR /
COPY --from=publish /otel-dotnet-auto ./otel-dotnet-auto 

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/bin/bash", "-c", "source /otel-dotnet-auto/instrument.sh && dotnet Clients.Api.dll"]

