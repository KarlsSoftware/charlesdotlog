using System.Net;
using System.Net.Http.Json;
using MinimalApiDemo.Models;
using Xunit;

namespace Backend.Tests;

// These tests cover the PUBLIC endpoints — no authentication required.
// They verify what anonymous visitors (readers of the blog) experience.
public class PostsPublicTests : IClassFixture<BlogApiFactory>, IAsyncLifetime
{
    private readonly BlogApiFactory _factory;
    private readonly HttpClient _client;

    public PostsPublicTests(BlogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // IAsyncLifetime.InitializeAsync runs BEFORE each test method.
    // We clear the database here so every test starts with a predictable empty state.
    public Task InitializeAsync()
    {
        _factory.ClearPosts();
        return Task.CompletedTask;
    }

    // IAsyncLifetime.DisposeAsync runs AFTER each test method.
    // Nothing to tear down in this class, but we must implement the interface.
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPosts_ReturnsOnlyPublishedPosts()
    {
        // Arrange: one published post and one draft
        _factory.SeedPost(title: "Published Post", isPublished: true);
        _factory.SeedPost(title: "Draft Post", isPublished: false);

        // Act
        var response = await _client.GetAsync("/api/posts");

        // Assert: 200 OK
        response.EnsureSuccessStatusCode();

        var posts = await response.Content.ReadFromJsonAsync<BlogPost[]>();
        Assert.NotNull(posts);

        // Only the published post should appear — the draft must be hidden from public
        Assert.Single(posts);
        Assert.Equal("Published Post", posts[0].Title);
    }

    [Fact]
    public async Task GetPosts_ReturnsPostsOrderedByNewestFirst()
    {
        // Arrange: two published posts seeded in "old then new" order
        // Because CreatedAt defaults to DateTime.UtcNow at the moment of seeding,
        // we add a small delay so the timestamps are actually different.
        _factory.SeedPost(title: "Older Post");
        await Task.Delay(10); // tiny pause so CreatedAt values differ
        _factory.SeedPost(title: "Newer Post");

        // Act
        var response = await _client.GetAsync("/api/posts");
        var posts = await response.Content.ReadFromJsonAsync<BlogPost[]>();
        Assert.NotNull(posts);

        // The API orders by CreatedAt descending — newest post must come first
        Assert.Equal("Newer Post", posts[0].Title);
        Assert.Equal("Older Post", posts[1].Title);
    }

    [Fact]
    public async Task GetPostById_WithPublishedPost_Returns200AndPost()
    {
        // Arrange: seed a published post and remember its Id
        var post = _factory.SeedPost(title: "Hello World");

        // Act
        var response = await _client.GetAsync($"/api/posts/{post.Id}");

        // Assert: 200 and the correct post data
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BlogPost>();
        Assert.NotNull(result);
        Assert.Equal(post.Id, result.Id);
        Assert.Equal("Hello World", result.Title);
    }

    [Fact]
    public async Task GetPostById_WithDraft_Returns404()
    {
        // A draft post must be invisible to public readers.
        // Accessing a draft URL directly must return 404, not 403 —
        // we don't even want to confirm the post exists.
        var draft = _factory.SeedPost(title: "Unpublished Draft", isPublished: false);

        var response = await _client.GetAsync($"/api/posts/{draft.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPostById_WithNonExistentId_Returns404()
    {
        // There is no post with Id 99999 — the API must return 404, not crash.
        var response = await _client.GetAsync("/api/posts/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchPosts_ByTitle_ReturnsMatchingPosts()
    {
        // Arrange: two posts with different titles
        _factory.SeedPost(title: "Angular Tutorial");
        _factory.SeedPost(title: "ASP.NET Core Guide");

        // Act: search by the word "Angular"
        var response = await _client.GetAsync("/api/posts/search?title=Angular");
        var posts = await response.Content.ReadFromJsonAsync<BlogPost[]>();
        Assert.NotNull(posts);

        // Only the post whose title contains "Angular" should be returned
        Assert.Single(posts);
        Assert.Contains("Angular", posts[0].Title);
    }

    [Fact]
    public async Task SearchPosts_ByAuthor_ReturnsMatchingPosts()
    {
        // Arrange: two posts by different authors
        _factory.SeedPost(title: "Post A", author: "Alice");
        _factory.SeedPost(title: "Post B", author: "Bob");

        // Act: search by author "Alice"
        var response = await _client.GetAsync("/api/posts/search?author=Alice");
        var posts = await response.Content.ReadFromJsonAsync<BlogPost[]>();
        Assert.NotNull(posts);

        // Only Alice's post should be returned
        Assert.Single(posts);
        Assert.Equal("Alice", posts[0].Author);
    }
}
