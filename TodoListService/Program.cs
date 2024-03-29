using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetValue<string>("DbConnectionString");
builder.Services.AddDbContext<TodoListDbContext>(opt => opt.UseSqlServer(dbConnectionString));
builder.Services.AddHttpClient<TodoItemService>(opt => opt.BaseAddress = new Uri("http://todo-item-svc"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetryTracing(otelBuilder => otelBuilder
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "MyCompany.MyProject.TodoListService"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddOtlpExporter());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/todo-lists", async ([FromServices] TodoListDbContext db, CancellationToken ct) => await db.TodoLists.ToListAsync(ct));

app.MapPost("/todo-lists", async ([FromBody] CreateTodoListCommand command, [FromServices] TodoListDbContext db, CancellationToken ct) =>
{
    var todoList = new TodoList { Name = command.Name };
    db.TodoLists.Add(todoList);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/todo-lists/{todoList.Id}", todoList);
});

app.MapGet("/todo-lists/{id:guid}", async ([FromRoute] Guid id, [FromServices] TodoListDbContext db, [FromServices] TodoItemService itemService,
    CancellationToken ct) =>
{
    var todoList = await db.TodoLists.FindAsync(new object[] { id }, ct);
    if (todoList is null) return Results.NotFound();
    var todos = await itemService.GetTodosByListId(id, ct);
    return Results.Ok(new GetTodoListResponse
    (
        Id: todoList.Id,
        Name: todoList.Name,
        Todos: todos
    ));
});

using (var dbInitScope = app.Services.CreateScope())
{
    var db = dbInitScope.ServiceProvider.GetRequiredService<TodoListDbContext>();
    var logger = dbInitScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var created = await db.Database.EnsureCreatedAsync();
    if (created)
    {
        logger.LogInformation("Created database and tables for EF Core");
    }
    else
    {
        logger.LogInformation("Database already exists");
    }
}

app.Run();

public record CreateTodoListCommand(string Name);
public record GetTodoListResponse(Guid Id, string? Name, List<TodoListItem> Todos);
public record TodoListItem
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public bool Completed { get; init; }
}

public class TodoList
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class TodoListDbContext : DbContext
{
    public TodoListDbContext(DbContextOptions<TodoListDbContext> options) : base(options) { }
    public DbSet<TodoList> TodoLists => Set<TodoList>();
}

public class TodoItemService
{
    private readonly HttpClient _httpClient;
    private static JsonSerializerOptions SerializerOpts = new() { PropertyNameCaseInsensitive = true };

    public TodoItemService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<List<TodoListItem>> GetTodosByListId(Guid listId, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"/todos?listId={listId}", ct);
        response.EnsureSuccessStatusCode();
        var todos = JsonSerializer.Deserialize<List<TodoListItem>>(
            await response.Content.ReadAsStreamAsync(ct), SerializerOpts);
        return todos ?? new List<TodoListItem>();
    }
}
