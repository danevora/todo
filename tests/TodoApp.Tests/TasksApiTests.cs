using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TodoApp.Tests;

/// <summary>
/// Integration tests over the real HTTP endpoints. These cover the two highest-risk
/// areas for this app: input validation (rejecting bad data) and that core CRUD +
/// soft-delete actually persist round-trip.
/// Ownership tests are intentionally absent — authentication is out of scope (see README).
/// </summary>
public class TasksApiTests : IClassFixture<TodoAppFactory>
{
    private readonly HttpClient _client;

    public TasksApiTests(TodoAppFactory factory) => _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---- Validation: bad input is rejected, not silently accepted ----

    [Fact]
    public async Task Create_WithEmptyTitle_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithWhitespaceTitle_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidStatus_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title = "Valid", status = "Nonsense" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidPriority_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title = "Valid", priority = "Urgent" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithInvalidPageSize_Returns400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/tasks?pageSize=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/tasks?pageSize=999")).StatusCode);
    }

    [Fact]
    public async Task Create_WithOverlongNotes_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Valid", notes = new string('x', 5001) });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Patch_WithOverlongNotes_Returns400()
    {
        var id = await CreateTask("Valid");
        var res = await _client.PatchAsJsonAsync($"/api/tasks/{id}",
            new { notes = new string('x', 5001) });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Core CRUD + soft-delete persist round-trip ----

    [Fact]
    public async Task Create_ValidTask_Returns201AndIsRetrievable()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Buy milk", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var created = await ReadTask(res);
        var id = created.GetProperty("id").GetInt32();

        var fetched = await _client.GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var task = await ReadTask(fetched);
        Assert.Equal("Buy milk", task.GetProperty("title").GetString());
        Assert.Equal("High", task.GetProperty("priority").GetString());
        Assert.Equal("Todo", task.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Patch_ChangesStatus_PersistsAcrossReads()
    {
        var id = await CreateTask("Write report");

        var patch = await _client.PatchAsJsonAsync($"/api/tasks/{id}", new { status = "Done" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var fetched = await ReadTask(await _client.GetAsync($"/api/tasks/{id}"));
        Assert.Equal("Done", fetched.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Patch_WithInvalidStatus_Returns400()
    {
        var id = await CreateTask("Has a valid title");
        var res = await _client.PatchAsJsonAsync($"/api/tasks/{id}", new { status = "Bogus" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndRemovesFromList()
    {
        var id = await CreateTask("Temporary task");

        var del = await _client.DeleteAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Gone individually...
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{id}")).StatusCode);

        // ...and excluded from the list (soft-delete query filter works).
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/tasks?pageSize=100", Json);
        var ids = list.GetProperty("items").EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32());
        Assert.DoesNotContain(id, ids);
    }

    [Fact]
    public async Task Patch_NonExistentTask_Returns404()
    {
        var res = await _client.PatchAsJsonAsync("/api/tasks/999999", new { status = "Done" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Patch_WithExplicitNull_ClearsNotesAndDueDate()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Has extras", notes = "some notes", dueDate = "2026-06-18" });
        var id = (await ReadTask(res)).GetProperty("id").GetInt32();

        // Explicit nulls (present in the body) should clear the fields...
        var patch = await _client.PatchAsJsonAsync($"/api/tasks/{id}",
            new { notes = (string?)null, dueDate = (string?)null });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var task = await ReadTask(await _client.GetAsync($"/api/tasks/{id}"));
        Assert.Equal(JsonValueKind.Null, task.GetProperty("notes").ValueKind);
        Assert.Equal(JsonValueKind.Null, task.GetProperty("dueDate").ValueKind);
    }

    [Fact]
    public async Task Patch_WithOmittedFields_LeavesThemUnchanged()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Keep notes", notes = "keep me" });
        var id = (await ReadTask(res)).GetProperty("id").GetInt32();

        // Body omits notes entirely — it must not be wiped.
        await _client.PatchAsJsonAsync($"/api/tasks/{id}", new { status = "Done" });

        var task = await ReadTask(await _client.GetAsync($"/api/tasks/{id}"));
        Assert.Equal("keep me", task.GetProperty("notes").GetString());
        Assert.Equal("Done", task.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DueDate_RoundTripsAsTheSameCalendarDay()
    {
        // The date-only contract: the day stored is the day returned, no timezone shift.
        var res = await _client.PostAsJsonAsync("/api/tasks",
            new { title = "Due date task", dueDate = "2026-06-18" });
        var id = (await ReadTask(res)).GetProperty("id").GetInt32();

        var task = await ReadTask(await _client.GetAsync($"/api/tasks/{id}"));
        Assert.StartsWith("2026-06-18", task.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Restore_BringsBackASoftDeletedTask()
    {
        var id = await CreateTask("Deleted then restored");
        await _client.DeleteAsync($"/api/tasks/{id}");

        var restore = await _client.PostAsync($"/api/tasks/{id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

        // Reachable again and back in the list.
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/tasks/{id}")).StatusCode);
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/tasks?pageSize=100", Json);
        var ids = list.GetProperty("items").EnumerateArray().Select(t => t.GetProperty("id").GetInt32());
        Assert.Contains(id, ids);
    }

    [Fact]
    public async Task Restore_OnANonDeletedTask_Returns404()
    {
        var id = await CreateTask("Still alive");
        var restore = await _client.PostAsync($"/api/tasks/{id}/restore", null);
        Assert.Equal(HttpStatusCode.NotFound, restore.StatusCode);
    }

    [Fact]
    public async Task GetAll_SortByPriority_OrdersBySeverityNotAlphabetically()
    {
        foreach (var p in new[] { "High", "Low", "Medium" })
            await _client.PostAsJsonAsync("/api/tasks", new { title = $"{p} task", priority = p });

        var list = await _client.GetFromJsonAsync<JsonElement>(
            "/api/tasks?sortBy=priority&sortDesc=false&pageSize=100", Json);
        var priorities = list.GetProperty("items").EnumerateArray()
            .Select(t => t.GetProperty("priority").GetString())
            .Where(p => p is "Low" or "Medium" or "High")
            .ToList();

        // Ascending must be Low < Medium < High, never alphabetical (High, Low, Medium).
        var lowIdx = priorities.IndexOf("Low");
        var medIdx = priorities.IndexOf("Medium");
        var highIdx = priorities.IndexOf("High");
        Assert.True(lowIdx < medIdx && medIdx < highIdx,
            $"Expected Low < Medium < High, got: {string.Join(", ", priorities)}");
    }

    // ---- helpers ----

    private async Task<int> CreateTask(string title)
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title });
        res.EnsureSuccessStatusCode();
        return (await ReadTask(res)).GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> ReadTask(HttpResponseMessage res) =>
        await res.Content.ReadFromJsonAsync<JsonElement>(Json);
}
