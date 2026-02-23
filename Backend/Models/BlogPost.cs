using System.ComponentModel.DataAnnotations;

namespace MinimalApiDemo.Models;

// Main entity — one instance of this class maps to one row in the BlogPosts table.
public class BlogPost
{
    // EF Core uses "Id" by naming convention as the primary key (auto-incremented integer).
    public int Id { get; set; }

    // [Required] → EF Core marks the column NOT NULL and model validation rejects empty values.
    // [StringLength(200)] → sets the column's max length in the database.
    // = string.Empty → default prevents C# nullable reference type warnings without using "?".
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Author { get; set; } = string.Empty;

    // Defaults to true so a new post is immediately visible unless explicitly set to false.
    public bool IsPublished { get; set; } = true;

    // Always store UTC — server timezone doesn't affect the value,
    // and date comparisons stay consistent regardless of where the server runs.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// DTOs (Data Transfer Objects) are what callers send in the HTTP request body.
// Using separate DTOs instead of the entity means callers can never accidentally
// supply Id or CreatedAt — those are always set server-side.
//
// `record` gives a concise positional constructor and value-based equality by default
// (two records with the same field values are considered equal — useful for tests).
public record CreateBlogPostDto(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    [Required, StringLength(100)] string Author,
    bool IsPublished = true  // optional — omitting it defaults to published
);

// Update DTO: same fields as Create, but IsPublished has no default —
// the caller must always explicitly provide the current publish state.
public record UpdateBlogPostDto(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    [Required, StringLength(100)] string Author,
    bool IsPublished
);
