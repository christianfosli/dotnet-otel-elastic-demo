using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetValue<string>("DbConnectionString");
builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseSqlServer(dbConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/todos", async (Guid? listId, bool? completed, TodoDbContext db, CancellationToken ct) =>
{
    var todos = await db.Todos
        .Where(todo => listId == null || todo.ListId == listId)
        .Where(todo => completed == null || todo.Completed == completed)
        .ToListAsync(ct);
    
    return Results.Ok(todos);
});

app.MapPost("/todos", async ([FromBody] CreateTodoCommand command, TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = new TodoItem { Name = command.Name, ListId = command.ListId };
    db.Todos.Add(todoItem);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/todos/{todoItem.Id}", todoItem);
});

app.MapGet("/todos/{id:guid}", async ([FromRoute] Guid id, TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = await db.Todos.FindAsync(new object[] { id }, ct);
    if (todoItem is null) return Results.NotFound();
    return Results.Ok(todoItem);
});

app.MapPut("/todos/{id:guid}/complete", async ([FromRoute] Guid id, TodoDbContext db, CancellationToken ct) =>
{
    var todoItem = await db.Todos.FindAsync(new object[] {id}, ct);
    if (todoItem is null) return Results.NotFound();
    todoItem.Completed = true;
    await db.SaveChangesAsync(ct);
    return Results.Ok();
});

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
