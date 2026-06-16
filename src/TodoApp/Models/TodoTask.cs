namespace TodoApp.Models;

public enum TodoTaskStatus
{
    Todo,
    InProgress,
    Done
}

public enum Priority
{
    Low,
    Medium,
    High
}

public class TodoTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public TodoTaskStatus Status { get; set; } = TodoTaskStatus.Todo;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
