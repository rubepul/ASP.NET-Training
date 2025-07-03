/* 
  The builder variable is the web application builder that 
  provides API's for configuring the application host.

  It sets up the host, configuration, and services needed to run the app.
*/
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

/* 
  The app variable defines the web application which is used to 
  configure the request-response (HTTP) pipeline, and routing.

  It sets up middleware, endpoints, and request handling based on
  the services and configuration provided by the builder.
*/
var app = builder.Build();

/* 
  Todo list is being stored in memory, not being saved to an 
  external database. So as soon as the application server is stopped
  the todos are wiped out.
*/
var todos = new List<Todo>();


// Implementing Web APIs

app.MapGet("/todos/", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
  var targetTodo = todos.SingleOrDefault(t => id == t.Id);
  return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) =>
{
  todos.Add(task);
  return TypedResults.Created("/todos/{id}", task);
});

app.MapDelete("todos/{id}", (int id) =>
{
  todos.RemoveAll(t => id == t.Id);
  return TypedResults.NoContent();
});



/*
  Starts the web server and begins processing incoming HTTP requests
  using the configured pipeline.
*/
app.Run();

// Todo type
public record Todo(int Id, string Name, DateTime dueDate, bool IsCompleted) {};

