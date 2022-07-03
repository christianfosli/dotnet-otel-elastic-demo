using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetValue<string>("DbConnectionString");
builder.Services.AddDbContext<TodoListDbContext>(opt => opt.UseSqlServer(dbConnectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/todo-lists", async (TodoListDbContext db, CancellationToken ct) => await db.TodoLists.ToListAsync(ct));

app.MapPost("/todo-lists", async ([FromBody] CreateTodoListCommand command, TodoListDbContext db, CancellationToken ct) =>
{
    var todoList = new TodoList { Name = command.Name };
    db.TodoLists.Add(todoList);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/todo-lists/{todoList.Id}", todoList);
});

app.MapGet("/todo-lists/{id:guid}", async ([FromRoute] Guid id, TodoListDbContext db, TodoItemService itemService,
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

app.Run();

record CreateTodoListCommand(string Name);
record GetTodoListResponse(Guid Id, string? Name, List<TodoListItem> Todos);
record TodoListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public bool Completed { get; init; }
}

public class TodoList
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

class TodoListDbContext : DbContext
{
    public TodoListDbContext(DbContextOptions<TodoListDbContext> options) : base(options) { }
    public DbSet<TodoList> TodoLists => Set<TodoList>();
}

class TodoItemService
{
    private readonly HttpClient _httpClient;
    public TodoItemService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<List<TodoListItem>> GetTodosByListId(Guid listId, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"/todos?listId={listId}", ct);
        response.EnsureSuccessStatusCode();
        var todos = JsonSerializer.Deserialize<List<TodoListItem>>(await response.Content.ReadAsStreamAsync(ct));
        return todos ?? new List<TodoListItem>();
    }
}
