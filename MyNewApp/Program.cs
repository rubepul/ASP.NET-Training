/* 
  The builder variable is the web application builder that 
  provides API's for configuring the application host.

  It sets up the host, configuration, and services needed to run the app.
  Think of builder as the place where you define how your app should be built.
*/
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

// Registering new service
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

/* 
  The app variable defines the web application which is used to 
  configure the request-response (HTTP) pipeline, and routing.

  It sets up middleware, endpoints, and request handling based on
  the services and configuration provided by the builder. Essentially, how the app 
  behaves when a request comes in.
  
*/
var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
  await next(context);
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});


/* 
  Todo list is being stored in memory, not being saved to an 
  external database. So as soon as the application server is stopped
  the todos are wiped out.
*/
var todos = new List<Todo>();


// Implementing Web APIs

app.MapGet("/todos/", (ITaskService service) => service.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
  var targetTodo = service.GetTodoById(id);
  return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task, ITaskService service) =>
{
  service.AddTodo(task);
  return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) => {
  var taskArgument = context.GetArgument<Todo>(0);
  var errors = new Dictionary<string, string[]>();
  if (taskArgument.dueDate < DateTime.UtcNow)
  {
    errors.Add(nameof(Todo.dueDate), ["Cannot have due date in the past."]);
  }
  if (taskArgument.IsCompleted)
  {
    errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
  }
  if (errors.Count > 0)
  {
    return Results.ValidationProblem(errors);
  }
  return await next(context);
  
});

app.MapDelete("todos/{id}", (int id, ITaskService service) =>
{
  service.DeleteTodoById(id);
  return TypedResults.NoContent();
});



/*
  Starts the web server and begins processing incoming HTTP requests
  using the configured pipeline.
*/
app.Run();



// Todo type
public record Todo(int Id, string Name, DateTime dueDate, bool IsCompleted) { };

// Defining the interface that represents the common functionality of our tasks service.
interface ITaskService
{
  Todo? GetTodoById(int id);

  List<Todo> GetTodos();

  void DeleteTodoById(int id);

  Todo AddTodo(Todo task);
}

// Create concrete implementation of the ITaskService interface

class InMemoryTaskService : ITaskService
{
  private readonly List<Todo> _todos = [];

  public Todo? GetTodoById(int id)
  {
    return _todos.SingleOrDefault(task => id == task.Id);
  }
  public List<Todo> GetTodos()
  {
    return _todos;
  }

  public void DeleteTodoById(int id)
  {
    _todos.RemoveAll(task => id == task.Id);
  }

  public Todo AddTodo(Todo task)
  {
    _todos.Add(task);
    return task;
  }
}
