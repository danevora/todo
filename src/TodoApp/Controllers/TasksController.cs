using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;
using TodoApp.Services;

namespace TodoApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(TaskService tasks) : ControllerBase
{
    // Input validation lives in TaskService and surfaces as 400 via the
    // ValidationException middleware in Program.cs, so these actions stay thin.

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await tasks.GetAllAsync(page, pageSize, status, priority, sortBy, sortDesc, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var task = await tasks.GetByIdAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var created = await tasks.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchTaskRequest req, CancellationToken ct)
    {
        var updated = await tasks.PatchAsync(id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await tasks.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var restored = await tasks.RestoreAsync(id, ct);
        return restored is null ? NotFound() : Ok(restored);
    }
}
