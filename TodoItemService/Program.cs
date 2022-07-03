using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetValue<string>("DbConnectionString");
builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseSqlServer(dbConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetryTracing(otelBuilder => otelBuilder
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "MyCompany.MyProject.TodoItemService"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddOtlpExporter());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/todos", async ([FromQuery] Guid? listId, [FromQuery] bool? completed, [FromServices] TodoDbContext db, CancellationToken ct) =>
{
    var todos = await db.Todos
        .Where(todo => listId == null || todo.ListId == listId)
        .Where(todo => completed == null || todo.Completed == completed)
        .ToListAsync(ct);
    
    return Results.Ok(todos);
});

app.MapPost("/todos", async ([FromBody] CreateTodoCommand command, [FromServices] TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = new TodoItem { Name = command.Name, ListId = command.ListId };
    db.Todos.Add(todoItem);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/todos/{todoItem.Id}", todoItem);
});

app.MapGet("/todos/{id:guid}", async ([FromRoute] Guid id, [FromServices] TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = await db.Todos.FindAsync(new object[] { id }, ct);
    if (todoItem is null) return Results.NotFound();
    return Results.Ok(todoItem);
});

app.MapPut("/todos/{id:guid}/complete", async ([FromRoute] Guid id, [FromServices] TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = await db.Todos.FindAsync(new object[] {id}, ct);
    if (todoItem is null) return Results.NotFound();
    todoItem.Completed = true;
    await db.SaveChangesAsync(ct);
    return Results.Ok();
});

using (var dbInitScope = app.Services.CreateScope())
{
    var db = dbInitScope.ServiceProvider.GetRequiredService<TodoDbContext>();
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

record CreateTodoCommand(Guid? ListId, string Name);

class TodoItem
{
    public Guid Id { get; set; }
    public Guid? ListId { get; set; }
    public string? Name { get; set; }
    public bool Completed { get; set; } = false;
}

class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
