namespace TodoApp.Services;

/// <summary>
/// Thrown by <see cref="TaskService"/> when input fails a business rule.
/// Mapped to an HTTP 400 by the exception-handling middleware in Program.cs.
/// </summary>
public class ValidationException(string message) : Exception(message);
