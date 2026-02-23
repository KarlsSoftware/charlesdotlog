using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApiDemo.Data;
using MinimalApiDemo.Models;

// Load environment variables from .env BEFORE anything else —
// builder.Services below will need JWT_SECRET and ADMIN_PASSWORD
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Register EF Core with SQLite — creates one AppDbContext per HTTP request (Scoped lifetime)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=blog.db"));

// Allow the Angular frontend (port 4200) to call this API.
// Without CORS, browsers block cross-origin requests entirely.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// --- JWT setup ---
// Read the secret once and build the signing key.
// `key` is captured by the login handler below via closure — no need to re-read the env var there.
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// Tell ASP.NET Core how to validate incoming JWT tokens on protected endpoints
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,          // we don't use named issuers
            ValidateAudience = false,        // we don't restrict to specific audiences
            ValidateLifetime = true,         // reject expired tokens
            ValidateIssuerSigningKey = true, // verify the token was signed with our secret
            IssuerSigningKey = key
        };
    });
// Enables RequireAuthorization() on endpoints — must be called after AddAuthentication
builder.Services.AddAuthorization();

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Teaches Swagger UI what auth scheme exists → adds the "Authorize" button and lock icons
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    // Tells Swagger which endpoints require auth → sends the Authorization header automatically in "Try it out"
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"   // must match the name used in AddSecurityDefinition above
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// CORS must be first — sets response headers before auth middleware reads them
app.UseCors();

// Only expose Swagger in development — never ship interactive API docs to production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Order matters: authenticate first (who are you?), then authorize (are you allowed?)
app.UseAuthentication();
app.UseAuthorization();

// Health-check — quick way to confirm the API is up
app.MapGet("/", () => "MyBlog API is running!");

// Login — validates the admin password from env, returns a signed JWT on success.
// `key` comes from the outer scope via closure — already built above, no need to repeat.
app.MapPost("/api/auth/login", (LoginDto dto) =>
{
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
        ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is not set.");

    if (dto.Password != adminPassword)
        return Results.Unauthorized();

    // Build a JWT with an "Admin" role claim, signed with our secret key
    var token = new JwtSecurityToken(
        claims: new[] { new Claim(ClaimTypes.Role, "Admin") },
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
});

// Group all post endpoints under /api/posts to avoid repeating the prefix on every route
var posts = app.MapGroup("/api/posts");

// GET /api/posts — return all posts, newest first
posts.MapGet("/", async (AppDbContext db) =>
    TypedResults.Ok(await db.BlogPosts.OrderByDescending(p => p.CreatedAt).ToListAsync()));

// GET /api/posts/{id} — :int constraint means /api/posts/abc returns 404 without hitting this handler
posts.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.BlogPosts.FindAsync(id) is BlogPost post
        ? Results.Ok(post)
        : Results.NotFound(new { Message = $"Post {id} not found" }));

// GET /api/posts/search?title=...&author=... — both query params are optional
posts.MapGet("/search", async (string? title, string? author, AppDbContext db) =>
{
    // AsQueryable() lets us build the WHERE clause conditionally before hitting the database
    var query = db.BlogPosts.AsQueryable();

    if (!string.IsNullOrEmpty(title))
        query = query.Where(p => p.Title.Contains(title));
    if (!string.IsNullOrEmpty(author))
        query = query.Where(p => p.Author.Contains(author));

    return TypedResults.Ok(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
});

// POST /api/posts — create a new post (admin only)
posts.MapPost("/", async (CreateBlogPostDto dto, AppDbContext db) =>
{
    if (string.IsNullOrEmpty(dto.Title)) return Results.BadRequest("Title is required.");
    if (string.IsNullOrEmpty(dto.Content)) return Results.BadRequest("Content is required.");
    if (string.IsNullOrEmpty(dto.Author)) return Results.BadRequest("Author is required.");

    var post = new BlogPost
    {
        Title = dto.Title,
        Content = dto.Content,
        Author = dto.Author,
        IsPublished = dto.IsPublished
    };

    db.BlogPosts.Add(post);
    await db.SaveChangesAsync();

    // 201 Created with a Location header pointing to the new resource
    return TypedResults.Created($"/api/posts/{post.Id}", post);
}).RequireAuthorization();

// PUT /api/posts/{id} — full replacement: all fields must be provided (admin only)
posts.MapPut("/{id:int}", async (int id, UpdateBlogPostDto dto, AppDbContext db) =>
{
    if (string.IsNullOrEmpty(dto.Title)) return Results.BadRequest("Title is required.");
    if (string.IsNullOrEmpty(dto.Content)) return Results.BadRequest("Content is required.");
    if (string.IsNullOrEmpty(dto.Author)) return Results.BadRequest("Author is required.");

    var post = await db.BlogPosts.FindAsync(id);
    if (post is null)
        return Results.NotFound(new { Message = $"Post {id} not found" });

    post.Title = dto.Title;
    post.Content = dto.Content;
    post.Author = dto.Author;
    post.IsPublished = dto.IsPublished;

    await db.SaveChangesAsync();
    return Results.Ok(post);
}).RequireAuthorization();

// DELETE /api/posts/{id} — remove a post (admin only)
posts.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var post = await db.BlogPosts.FindAsync(id);
    if (post is null)
        return Results.NotFound(new { Message = $"Post {id} not found" });

    db.BlogPosts.Remove(post);
    await db.SaveChangesAsync();
    return Results.NoContent(); // 204 — success with no response body
}).RequireAuthorization();

app.Run();
