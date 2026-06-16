using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Translate business-rule failures into 400s with a plain-text message the UI can show.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(ex.Message);
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
