FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
# Enable OTEL Self-diagnostics (https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry#self-diagnostics)
RUN echo '{"LogDirectory": ".", "FileSize": 1024, "LogLevel": "Warning"}' > OTEL_DIAGNOSTICS.json
ENTRYPOINT ["dotnet", "TodoListService.dll"]
