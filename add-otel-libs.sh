cd TodoListService
dotnet add package --prerelease OpenTelemetry.Extensions.Hosting
dotnet add package --prerelease OpenTelemetry.Instrumentation.AspNetCore
dotnet add package --prerelease OpenTelemetry.Instrumentation.Http
dotnet add package --prerelease OpenTelemetry.Instrumentation.SqlClient
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.5
cd ../TodoItemService
dotnet add package --prerelease OpenTelemetry.Extensions.Hosting
dotnet add package --prerelease OpenTelemetry.Instrumentation.AspNetCore
dotnet add package --prerelease OpenTelemetry.Instrumentation.Http
dotnet add package --prerelease OpenTelemetry.Instrumentation.SqlClient
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.5
