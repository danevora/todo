namespace TodoApp.Models;

public record TaskDto(
    int Id,
    string Title,
    string? Notes,
    string Status,
    string Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateTaskRequest(
    string Title,
    string? Notes,
    string? Status,
    string? Priority,
    DateTime? DueDate
);

public record PatchTaskRequest(
    string? Title,
    string? Notes,
    string? Status,
    string? Priority,
    DateTime? DueDate
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int Total
);
