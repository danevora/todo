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

// Optional<> fields so a partial update can tell "leave unchanged" (omitted)
// apart from "clear this field" (explicit null) for the nullable Notes/DueDate.
public record PatchTaskRequest(
    Optional<string?> Title,
    Optional<string?> Notes,
    Optional<string?> Status,
    Optional<string?> Priority,
    Optional<DateTime?> DueDate
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int Total
);
