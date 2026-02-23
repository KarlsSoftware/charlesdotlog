using System.ComponentModel.DataAnnotations;

namespace MinimalApiDemo.Models;

// Main entity — represents a blog post
public class BlogPost
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Author { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// DTOs keep your API surface clean — never expose your entity directly
public record CreateBlogPostDto(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    [Required, StringLength(100)] string Author,
    bool IsPublished = true
);

public record UpdateBlogPostDto(
    [Required, StringLength(200)] string Title,
    [Required] string Content,
    [Required, StringLength(100)] string Author,
    bool IsPublished
);
