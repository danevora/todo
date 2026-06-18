using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoTask> Tasks => Set<TodoTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(500);
            e.Property(t => t.Notes).HasMaxLength(5000);
            // Status is stored as text (human-readable; never sorted on).
            e.Property(t => t.Status).HasConversion<string>();
            // Priority is stored as its underlying int (Low=0 < Medium=1 < High=2) so that
            // ORDER BY Priority sorts by severity, not alphabetically ("High,Low,Medium").
            e.Property(t => t.Priority).HasConversion<int>();
            // Global query filter — automatically excludes soft-deleted rows from all queries
            e.HasQueryFilter(t => t.DeletedAt == null);
        });
    }
}
