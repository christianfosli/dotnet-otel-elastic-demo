var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/todo-lists", () => "Hello World!");

app.Run();
