using Microsoft.EntityFrameworkCore;
using MinimalApiDemo.Models;

namespace MinimalApiDemo.Data;

// DbContext is EF Core's "unit of work" — it tracks every entity you load or create
// and translates LINQ queries into SQL automatically.
// One instance is created per HTTP request (Scoped lifetime, set in Program.cs).
public class AppDbContext : DbContext
{
    // Pass EF Core's configuration (database provider, connection string) up to the base class.
    // The options object is built in Program.cs via builder.Services.AddDbContext<...>().
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet<T> represents the BlogPosts table. The => Set<BlogPost>() expression-body form
    // is preferred over a plain { get; set; } auto-property because it avoids C# nullable
    // reference warnings and makes it clear this delegates to EF Core's internal tracking.
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}
