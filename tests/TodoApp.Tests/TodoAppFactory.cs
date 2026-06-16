using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApp.Data;

namespace TodoApp.Tests;

/// <summary>
/// Hosts the real app for integration tests, swapping the file-backed SQLite
/// database for a private in-memory one (a fresh, isolated DB per factory).
/// We use SQLite — not EF In-Memory — so the tests exercise real SQL semantics
/// and the same provider used in production.
/// </summary>
public class TodoAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContext registration with the in-memory connection.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
