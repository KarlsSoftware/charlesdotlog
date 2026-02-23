using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MinimalApiDemo.Models;
using Xunit;

namespace Backend.Tests;

// These tests cover all admin CRUD operations with a valid JWT token.
// They verify the "happy path" (things go right) AND error cases
// (invalid input, missing resources).
//
// IAsyncLifetime gives us async setup/teardown per test:
//   InitializeAsync → get a fresh JWT and clear the database
//   DisposeAsync    → nothing to do here
public class AdminCrudTests : IClassFixture<BlogApiFactory>, IAsyncLifetime
{
    private readonly BlogApiFactory _factory;
    private HttpClient _client = null!;

    public AdminCrudTests(BlogApiFactory factory)
    {
        _factory = factory;
    }

    // Runs before each test method.
    // Gets a fresh admin token and attaches it to every request this client makes.
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        // Step 1: log in as admin to get a JWT
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Password = "testpassword123" });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Step 2: attach the token as a Bearer header so all subsequent requests are authenticated
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Step 3: wipe the database so each test starts clean
        _factory.ClearPosts();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── GET /api/posts/admin ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminPosts_ReturnsAllPosts_IncludingDrafts()
    {
        // Arrange: one published + one draft
        _factory.SeedPost(title: "Published", isPublished: true);
        _factory.SeedPost(title: "Draft", isPublished: false);

        // Act
        var response = await _client.GetAsync("/api/posts/admin");
        response.EnsureSuccessStatusCode();

        var posts = await response.Content.ReadFromJsonAsync<BlogPost[]>();
        Assert.NotNull(posts);

        // Admin list must include BOTH posts — the public endpoint hides drafts, this one must not
        Assert.Equal(2, posts.Length);
    }

    // ─── GET /api/posts/admin/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetAdminPostById_WithDraft_Returns200()
    {
        // Admin can load a draft by its Id — this is required for the edit form
        var draft = _factory.SeedPost(title: "My Draft", isPublished: false);

        var response = await _client.GetAsync($"/api/posts/admin/{draft.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BlogPost>();
        Assert.NotNull(result);
        Assert.Equal(draft.Id, result.Id);
        Assert.False(result.IsPublished); // still a draft
    }

    [Fact]
    public async Task GetAdminPostById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/posts/admin/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── POST /api/posts ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePost_WithValidData_Returns201AndCreatedPost()
    {
        // Arrange: a valid new post
        var newPost = new
        {
            Title = "Brand New Post",
            Content = "<p>Content here</p>",
            Author = "Carlo",
            IsPublished = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", newPost);

        // Assert: 201 Created (not 200 OK — the API uses TypedResults.Created)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BlogPost>();
        Assert.NotNull(created);

        // The server must assign a real Id (not 0) and echo the data back
        Assert.True(created.Id > 0);
        Assert.Equal("Brand New Post", created.Title);
        Assert.Equal("Carlo", created.Author);
    }

    [Fact]
    public async Task CreatePost_WithMissingTitle_Returns400()
    {
        // Title is required — the API must reject requests without it
        var body = new { Title = "", Content = "<p>Some content</p>", Author = "Carlo", IsPublished = true };
        var response = await _client.PostAsJsonAsync("/api/posts", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithMissingContent_Returns400()
    {
        var body = new { Title = "A Title", Content = "", Author = "Carlo", IsPublished = true };
        var response = await _client.PostAsJsonAsync("/api/posts", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithMissingAuthor_Returns400()
    {
        var body = new { Title = "A Title", Content = "<p>Content</p>", Author = "", IsPublished = true };
        var response = await _client.PostAsJsonAsync("/api/posts", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/posts/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePost_WithValidData_Returns200AndUpdatedPost()
    {
        // Arrange: create a post to update
        var post = _factory.SeedPost(title: "Original Title", isPublished: true);

        var update = new
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>",
            Author = "New Author",
            IsPublished = false  // also testing that publish state can change
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/posts/{post.Id}", update);

        // Assert: 200 OK with the updated data echoed back
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BlogPost>();
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("New Author", result.Author);
        Assert.False(result.IsPublished); // publish state was changed to false
    }

    [Fact]
    public async Task UpdatePost_WithNonExistentId_Returns404()
    {
        var update = new { Title = "X", Content = "<p>X</p>", Author = "X", IsPublished = true };
        var response = await _client.PutAsJsonAsync("/api/posts/99999", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── DELETE /api/posts/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeletePost_WithExistingId_Returns204AndPostIsGone()
    {
        // Arrange: a post that exists
        var post = _factory.SeedPost(title: "Post To Delete");

        // Act: delete it
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{post.Id}");

        // Assert: 204 No Content (success, no response body)
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify the post is actually gone by trying to fetch it
        var getResponse = await _client.GetAsync($"/api/posts/admin/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePost_WithNonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/posts/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private record LoginResponse(string Token);
}
