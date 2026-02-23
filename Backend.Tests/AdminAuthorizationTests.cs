using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Backend.Tests;

// These tests verify that EVERY protected endpoint correctly rejects requests
// that have no Authorization header.
//
// Why test this separately? Authorization bugs are silent: if .RequireAuthorization()
// is accidentally removed from one endpoint, the app still "works" — it just leaks
// admin data. A dedicated suite catches that regression immediately.
public class AdminAuthorizationTests : IClassFixture<BlogApiFactory>
{
    private readonly HttpClient _client;

    public AdminAuthorizationTests(BlogApiFactory factory)
    {
        // CreateClient() returns a client with NO Authorization header.
        // Every request this client makes is "anonymous".
        _client = factory.CreateClient();
    }

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminPosts_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/posts/admin");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminPostById_WithoutToken_Returns401()
    {
        // Id 1 may or may not exist — auth is checked before the DB query,
        // so we still get 401 regardless.
        var response = await _client.GetAsync("/api/posts/admin/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithoutToken_Returns401()
    {
        var body = new { Title = "Hacked Post", Content = "<p>Oops</p>", Author = "Hacker" };
        var response = await _client.PostAsJsonAsync("/api/posts", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePost_WithoutToken_Returns401()
    {
        var body = new { Title = "Changed", Content = "<p>Changed</p>", Author = "Hacker", IsPublished = true };
        var response = await _client.PutAsJsonAsync("/api/posts/1", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/posts/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
