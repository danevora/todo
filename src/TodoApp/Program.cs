using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=todo.db";
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));
builder.Services.AddScoped<TaskService>();

builder.Services.AddCors(opts => opts.AddPolicy("Frontend", policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? ["http://localhost:5173"];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// Create the SQLite schema on first run (EnsureCreated, not migrations — see README).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Any unhandled exception becomes a clean ProblemDetails 500 (no stack trace leak, even in dev).
app.UseExceptionHandler();

// Business-rule failures become a ProblemDetails 400 carrying the message for the UI to show.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        await Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest,
            title: "Validation failed").ExecuteAsync(context);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.MapControllers();
app.Run();

// Exposed so the integration test project can host the app via WebApplicationFactory.
public partial class Program { }
