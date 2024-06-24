namespace Api.Todos;

using System.ComponentModel.DataAnnotations;
using Data;

public record class TodoItemViewModel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = default!;
    public bool IsComplete { get; set; }
}

public static class TodoMappingExtensions
{
    public static TodoItemViewModel AsTodoItem(this TodoItem todo) =>
        new()
        {
            Id = todo.Id,
            Title = todo.Title,
            IsComplete = todo.IsComplete,
        };
}
