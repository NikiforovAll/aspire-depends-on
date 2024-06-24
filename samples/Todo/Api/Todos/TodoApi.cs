namespace TodoApi;

using Api.Todos;
using Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

internal static class TodoApi
{
    public static RouteGroupBuilder MapTodos(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/todos");

        group.WithTags("Todos");

        group.WithParameterValidation(typeof(TodoItemViewModel));

        group
            .MapGet(
                "/",
                async (TodoDbContext db) =>
                    await db.Todos.Select(t => t.AsTodoItem()).AsNoTracking().ToListAsync()
            )
            .WithName("GetTodoItems")
            .WithOpenApi();

        group
            .MapGet(
                "/{id}",
                async Task<Results<Ok<TodoItemViewModel>, NotFound>> (TodoDbContext db, int id) =>
                    await db.Todos.FindAsync(id) switch
                    {
                        TodoItem todo => TypedResults.Ok(todo.AsTodoItem()),
                        _ => TypedResults.NotFound()
                    }
            )
            .WithName("GetTodoItem")
            .WithOpenApi();

        group
            .MapPost(
                "/",
                async Task<Created<TodoItemViewModel>> (
                    TodoDbContext db,
                    TodoItemViewModel newTodo
                ) =>
                {
                    var todo = new TodoItem
                    {
                        Title = newTodo.Title,
                        IsComplete = newTodo.IsComplete
                    };

                    db.Todos.Add(todo);
                    await db.SaveChangesAsync();

                    return TypedResults.Created($"/todos/{todo.Id}", todo.AsTodoItem());
                }
            )
            .WithName("CreateTodoItem")
            .WithOpenApi();

        group
            .MapPut(
                "/{id}",
                async Task<Results<Ok, NotFound, BadRequest>> (
                    TodoDbContext db,
                    int id,
                    TodoItemViewModel todo
                ) =>
                {
                    if (id != todo.Id)
                    {
                        return TypedResults.BadRequest();
                    }

                    var rowsAffected = await db.Todos.ExecuteUpdateAsync(updates =>
                        updates
                            .SetProperty(t => t.IsComplete, todo.IsComplete)
                            .SetProperty(t => t.Title, todo.Title)
                    );

                    return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                }
            )
            .WithName("UpdateTodoItem")
            .WithOpenApi();

        group
            .MapDelete(
                "/{id}",
                async Task<Results<NotFound, Ok>> (TodoDbContext db, int id) =>
                {
                    var rowsAffected = await db
                        .Todos.Where(t => t.Id == id)
                        .ExecuteDeleteAsync();

                    return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                }
            )
            .WithName("DeleteTodoItem")
            .WithOpenApi();

        return group;
    }
}
