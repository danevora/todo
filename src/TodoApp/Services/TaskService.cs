using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>
/// All task business logic and persistence. Talks to <see cref="AppDbContext"/> directly —
/// there is one data store and one consumer, so a separate repository layer would add
/// indirection without a second implementation to justify it.
/// </summary>
public class TaskService(AppDbContext db)
{
    private const int MaxTitleLength = 500;

    public async Task<PagedResult<TaskDto>> GetAllAsync(
        int page, int pageSize,
        string? status, string? priority,
        string? sortBy, bool sortDesc,
        CancellationToken ct = default)
    {
        if (page < 1) throw new ValidationException("page must be >= 1.");
        if (pageSize is < 1 or > 100) throw new ValidationException("pageSize must be between 1 and 100.");

        var statusEnum = ParseStatus(status);
        var priorityEnum = ParsePriority(priority);

        var query = db.Tasks.AsQueryable();
        if (statusEnum.HasValue) query = query.Where(t => t.Status == statusEnum.Value);
        if (priorityEnum.HasValue) query = query.Where(t => t.Priority == priorityEnum.Value);

        query = (sortBy?.ToLowerInvariant(), sortDesc) switch
        {
            ("title", false) => query.OrderBy(t => t.Title),
            ("title", true) => query.OrderByDescending(t => t.Title),
            ("duedate", false) => query.OrderBy(t => t.DueDate),
            ("duedate", true) => query.OrderByDescending(t => t.DueDate),
            ("priority", false) => query.OrderBy(t => t.Priority),
            ("priority", true) => query.OrderByDescending(t => t.Priority),
            (_, false) => query.OrderBy(t => t.CreatedAt),
            (_, true) => query.OrderByDescending(t => t.CreatedAt),
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<TaskDto>(items.Select(Map), page, pageSize, total);
    }

    public async Task<TaskDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        return task is null ? null : Map(task);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest req, CancellationToken ct = default)
    {
        var title = ValidateTitle(req.Title);
        var now = DateTime.UtcNow;

        var task = new TodoTask
        {
            Title = title,
            Notes = req.Notes,
            Status = ParseStatus(req.Status) ?? TodoTaskStatus.Todo,
            Priority = ParsePriority(req.Priority) ?? Priority.Medium,
            DueDate = req.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return Map(task);
    }

    public async Task<TaskDto?> PatchAsync(int id, PatchTaskRequest req, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null) return null;

        // Title/Status/Priority aren't nullable, so a null value is a no-op for them;
        // Notes/DueDate are clearable, so an explicit null (IsSet) wipes them.
        if (req.Title.IsSet) task.Title = ValidateTitle(req.Title.Value);
        if (req.Notes.IsSet) task.Notes = req.Notes.Value;
        if (req.Status is { IsSet: true, Value: not null }) task.Status = ParseStatus(req.Status.Value)!.Value;
        if (req.Priority is { IsSet: true, Value: not null }) task.Priority = ParsePriority(req.Priority.Value)!.Value;
        if (req.DueDate.IsSet) task.DueDate = req.DueDate.Value;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Map(task);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null) return false;
        task.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TaskDto?> RestoreAsync(int id, CancellationToken ct = default)
    {
        // The global query filter hides soft-deleted rows, so bypass it to find one to restore.
        var task = await db.Tasks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt != null, ct);
        if (task is null) return null;
        task.DeletedAt = null;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(task);
    }

    private static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim() ?? "";
        if (trimmed.Length == 0) throw new ValidationException("Title is required.");
        if (trimmed.Length > MaxTitleLength)
            throw new ValidationException($"Title must be {MaxTitleLength} characters or fewer.");
        return trimmed;
    }

    // A null/empty filter value means "no filter"; a non-empty but invalid value is a client error.
    private static TodoTaskStatus? ParseStatus(string? value) =>
        ParseEnum<TodoTaskStatus>(value, "status");

    private static Priority? ParsePriority(string? value) =>
        ParseEnum<Priority>(value, "priority");

    private static T? ParseEnum<T>(string? value, string field) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
            return result;
        throw new ValidationException(
            $"Invalid {field} '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<T>())}.");
    }

    private static TaskDto Map(TodoTask t) => new(
        t.Id, t.Title, t.Notes,
        t.Status.ToString(), t.Priority.ToString(),
        t.DueDate, t.CreatedAt, t.UpdatedAt);
}
