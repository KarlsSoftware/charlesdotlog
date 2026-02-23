# .NET Minimal API — Reference

## File Architecture

```
Backend/
├── Program.cs               # Entry point — services, middleware, and all endpoints
├── Data/
│   └── AppDbContext.cs      # EF Core DbContext
├── Models/
│   ├── BlogPost.cs          # Entity + DTOs (Create/Update)
│   └── LoginDto.cs
```

---

## Program.cs — Structure

No controllers. `Program.cs` is the single source of truth: register services, build, configure middleware, and map all endpoints inline.

```csharp
DotEnv.Load();                              // must be FIRST — env vars used by builder

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddCors(...);

// JWT
builder.Services.AddAuthentication(...).AddJwtBearer(...);
builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(...);

var app = builder.Build();

app.UseCors();                             // CORS before auth middleware
app.UseAuthentication();
app.UseAuthorization();

// Auth
app.MapGet("/", () => "MyBlog API is running!");
app.MapPost("/api/auth/login", ...);

// Posts
var posts = app.MapGroup("/api/posts");
posts.MapGet("/", ...);
// ...

app.Run();
```

Middleware order matters: `UseCors` → `UseAuthentication` → `UseAuthorization`.

---

## Dependency Injection

Services are registered on `builder.Services` before `builder.Build()`. The three lifetimes:

| Lifetime | Registration | Instance per |
|----------|-------------|--------------|
| `AddSingleton` | once at startup | application |
| `AddScoped` | per HTTP request | request |
| `AddTransient` | every time resolved | resolution |

EF Core's `AddDbContext` defaults to **Scoped** — one `AppDbContext` per request, which is correct (tracks changes within a single request, then gets disposed).

In minimal API handlers, registered services are injected as **parameters** — the framework resolves them automatically:

```csharp
posts.MapGet("/", async (AppDbContext db) =>          // db injected from DI
    TypedResults.Ok(await db.BlogPosts.ToListAsync()));

posts.MapGet("/{id:int}", async (int id, AppDbContext db) =>  // route param + service
    await db.BlogPosts.FindAsync(id) is BlogPost post
        ? Results.Ok(post)
        : Results.NotFound());
```

`int id` comes from the route; `AppDbContext db` comes from the DI container. The runtime distinguishes them by type.

---

## dotenv.net

`DotEnv.Load()` reads `.env` from the project root before `WebApplication.CreateBuilder` runs, making env vars available to everything including JWT secret retrieval.

Preferred over `appsettings.json` for secrets — `.env` stays out of source control.

```
JWT_SECRET=your-256-bit-secret
ADMIN_PASSWORD=yourpassword
```

---

## Route Groups

`MapGroup` avoids repeating prefixes and lets you attach auth to all endpoints at once:

```csharp
var posts = app.MapGroup("/api/posts");

posts.MapGet("/", ...);                   // → GET /api/posts
posts.MapGet("/{id:int}", ...);           // → GET /api/posts/{id}
posts.MapPost("/", ...).RequireAuthorization();
```

`/{id:int}` tells the router to only match if the segment is a valid integer — `/api/posts/abc` returns 404 without hitting the handler.

`RequireAuthorization()` can be set on the group (all endpoints) or chained on individual endpoints.

---

## Search Endpoint

Optional query parameters are bound by name automatically — no `[FromQuery]` attribute needed:

```csharp
// GET /api/posts/search?title=hello&author=carlo
posts.MapGet("/search", async (string? title, string? author, AppDbContext db) =>
{
    // AsQueryable() defers execution — no DB call yet, just building the query object
    var query = db.BlogPosts.AsQueryable();

    if (!string.IsNullOrEmpty(title))
        query = query.Where(p => p.Title.Contains(title));
    if (!string.IsNullOrEmpty(author))
        query = query.Where(p => p.Author.Contains(author));

    // EF Core translates the full chain to a single SQL SELECT with WHERE clauses
    return TypedResults.Ok(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
});
```

`AsQueryable()` returns an `IQueryable<T>` instead of evaluating the query immediately. Each `.Where()` call adds a SQL `WHERE` clause — EF Core sends one combined query to the database, not multiple round-trips.

---

## TypedResults vs Results

`TypedResults.Ok(value)` includes the response type in OpenAPI metadata — better for Swagger docs. `Results.Ok(value)` is untyped; use it when a handler can return different response types (e.g., `Ok` vs `NotFound`). Both work identically at runtime.

---

## Inline Validation

The project validates manually in each handler rather than using a filter:

```csharp
posts.MapPost("/", async (CreateBlogPostDto dto, AppDbContext db) =>
{
    if (string.IsNullOrEmpty(dto.Title)) return Results.BadRequest("Title is required.");
    if (string.IsNullOrEmpty(dto.Content)) return Results.BadRequest("Content is required.");
    if (string.IsNullOrEmpty(dto.Author)) return Results.BadRequest("Author is required.");
    // ...
}).RequireAuthorization();
```

---

## LoginDto

`LoginDto` lives in `Models/LoginDto.cs` — it's the request body shape for `POST /api/auth/login`:

```csharp
// `record` is concise for DTOs that are just data containers with no behavior.
// Single-field — no username, just a password (single-admin blog).
public record LoginDto(string Password);
```

---

## Null-Coalescing Throw (`?? throw`)

Used throughout Program.cs to fail fast if required config is missing:

```csharp
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
```

`??` returns the left side if it's non-null, otherwise evaluates the right side. Throwing in the right side means the app crashes at startup with a clear message rather than failing later with a cryptic `NullReferenceException`.

---

## Models & DTOs

The entity is never exposed directly — DTOs control the API surface and carry validation attributes.

```csharp
// Entity
public class BlogPost
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required]                    public string Content { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Author { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Create DTO — record keeps it concise
public record CreateBlogPostDto(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    [Required, StringLength(100)] string Author,
    bool IsPublished = true
);

```

`record` gives value equality and immutability by default — good for DTOs that are just data containers.

---

## Entity Framework Core

```csharp
// Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}
```

Registration — EF uses scoped lifetime by default (one instance per request):

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=blog.db"));
```

Async LINQ — always use the async variants to avoid blocking the thread pool:

```csharp
await db.BlogPosts.ToListAsync()
await db.BlogPosts.FindAsync(id)                              // lookup by PK
await db.BlogPosts.OrderByDescending(p => p.CreatedAt).ToListAsync()
await db.SaveChangesAsync()                                   // INSERT / UPDATE / DELETE
```

---

## CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
            "http://localhost:4200",           // local Angular dev server
            "https://carlodotlog.vercel.app"   // production frontend on Vercel
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Pipeline — must be before UseAuthentication
app.UseCors();   // no args = use default policy
```

The browser blocks cross-origin requests unless the server sends `Access-Control-Allow-Origin` headers that match the requesting origin. `WithOrigins(...)` whitelists specific domains — any request from an unlisted origin is rejected by the browser before it even reaches your API logic.

---

## Swagger / OpenAPI

Swagger UI is a web page that makes HTTP requests from the browser — it has no idea the API uses JWT unless you explicitly describe the scheme.

Two calls, two different jobs:

**`AddSecurityDefinition`** — teaches Swagger *what kind of auth exists*. This adds the "Authorize" button and lock icons to the UI.

**`AddSecurityRequirement`** — tells Swagger *which endpoints need it*. This makes "Try it out" automatically send `Authorization: Bearer <token>`.

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter your JWT token"
});

options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }
});
```

The verbose `OpenApiSecurityScheme` inside `AddSecurityRequirement` is a pointer back to the definition by name (`Id = "Bearer"`). It's boilerplate you copy once. Swagger is only enabled in development (`app.Environment.IsDevelopment()`).

---

## JWT Authentication

Full flow inline in `Program.cs`:

```csharp
// 1. Read secret and build key — done once at startup
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// 2. Service registration — configures bearer validation using the key above
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key
        };
    });
builder.Services.AddAuthorization();

// 3. Login endpoint — validates password, returns signed JWT
// `key` is captured from the outer scope via closure — no need to re-read the env var
app.MapPost("/api/auth/login", (LoginDto dto) =>
{
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is not set.");

    if (dto.Password != adminPassword)
        return Results.Unauthorized();

    var token = new JwtSecurityToken(
        claims: new[] { new Claim(ClaimTypes.Role, "Admin") },
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
});
```

```csharp
// 3. Middleware pipeline — order matters
app.UseAuthentication();
app.UseAuthorization();

// 4. Protect individual endpoints
posts.MapPost("/", handler).RequireAuthorization();
posts.MapPut("/{id:int}", handler).RequireAuthorization();
posts.MapDelete("/{id:int}", handler).RequireAuthorization();
```

Missing/invalid token → `401 Unauthorized`. Valid token, wrong role → `403 Forbidden`.
