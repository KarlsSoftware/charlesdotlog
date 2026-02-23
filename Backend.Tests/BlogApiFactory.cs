using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MinimalApiDemo.Data;
using MinimalApiDemo.Models;

namespace Backend.Tests;

// WebApplicationFactory<Program> starts the REAL ASP.NET Core app inside the test process.
// No separate server is needed — HTTP requests go through an in-process test handler.
// This is called an "integration test": it tests the full request pipeline (routing,
// middleware, auth, EF Core) all at once.
public class BlogApiFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own isolated database.
    // Guid.NewGuid() produces a unique name, so test classes never share state.
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    // Static constructor runs ONCE when the type is first used — before the app starts.
    // We set the environment variables here so Program.cs can read them during startup.
    // dotenv.net's DotEnv.Load() will also load the .env file copied to the output dir,
    // but setting them here too is a safety net.
    static BlogApiFactory()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-that-is-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "testpassword123");
    }

    // ConfigureWebHost lets us override services AFTER Program.cs has set them up.
    // Perfect for swapping the real database with a test database.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Run in Development so Swagger is enabled (prevents environment-specific startup errors).
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // --- Swap SQLite for an in-memory database ---
            //
            // In EF Core 9, AddDbContext() registers options under
            // IDbContextOptionsConfiguration<TContext> (NOT DbContextOptions<TContext>).
            // If we just call AddDbContext again, EF Core sees BOTH the SQLite and InMemory
            // configurations and throws "multiple providers" at runtime.
            //
            // Fix: remove all existing IDbContextOptionsConfiguration<AppDbContext>
            // registrations before adding our InMemory one.
            var configType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var existingConfigs = services
                .Where(d => d.ServiceType == configType)
                .ToList();

            foreach (var config in existingConfigs)
                services.Remove(config);

            // Add a fresh DbContext backed by an in-memory store.
            // UseInMemoryDatabase is fast, requires no file, and is automatically
            // cleaned up when the factory is disposed.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    // ─── Seed helpers ─────────────────────────────────────────────────────────

    // Creates a BlogPost in the database and returns it with the auto-assigned Id.
    // Tests use this to set up preconditions without calling the HTTP API.
    public BlogPost SeedPost(
        string title = "Test Post",
        string content = "<p>Hello world</p>",
        string author = "Tester",
        bool isPublished = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var post = new BlogPost
        {
            Title = title,
            Content = content,
            Author = author,
            IsPublished = isPublished
        };

        db.BlogPosts.Add(post);
        db.SaveChanges();
        return post;
    }

    // Deletes every post so each test starts with a clean slate.
    public void ClearPosts()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.BlogPosts.RemoveRange(db.BlogPosts);
        db.SaveChanges();
    }
}
