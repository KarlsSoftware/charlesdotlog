using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Backend.Tests;

// IClassFixture<T> tells xUnit: create ONE BlogApiFactory for this entire test class
// and share it across all test methods. The app starts once, which is much faster
// than restarting it for every [Fact].
public class AuthTests : IClassFixture<BlogApiFactory>
{
    private readonly HttpClient _client;

    // This password must match the ADMIN_PASSWORD set in BlogApiFactory's static ctor
    // and in the .env file in the test project.
    private const string CorrectPassword = "testpassword123";

    public AuthTests(BlogApiFactory factory)
    {
        // CreateClient() creates an HttpClient that talks to the in-process test server.
        // No network port is opened — it's all in memory.
        _client = factory.CreateClient();
    }

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200AndToken()
    {
        // Arrange: build the request body
        var body = new { Password = CorrectPassword };

        // Act: POST to the login endpoint
        var response = await _client.PostAsJsonAsync("/api/auth/login", body);

        // Assert: HTTP 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: response body contains a non-empty Token string
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        // A wrong password must NEVER return a token — 401 is the only acceptable response.
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Password = "completely-wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns401()
    {
        // An empty string is not the admin password and must be rejected.
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Password = string.Empty });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    // record = immutable data object with automatic constructor and equality.
    // Used only to deserialize the login response JSON: { "token": "..." }
    private record LoginResponse(string Token);
}
