
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi;
using TodoApi.Users;
using Xunit;

public class UsersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var newUser = new CreateUserRequest
        {
            Username = $"user_{userId}",
            Password = "P@ssw0rd!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidUser_ReturnsBadRequest()
    {
        // Arrange
        var newUser = new CreateUserRequest
        {
            Username = "",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateToken_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var newUser = new CreateUserRequest
        {
            Username = $"user_{userId}",
            Password = "P@ssw0rd!"
        };
        await _client.PostAsJsonAsync("/users", newUser);

        // Act
        var response = await _client.PostAsJsonAsync("/users/token", newUser);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokenResponse?.Token);
    }

    [Fact]
    public async Task GenerateToken_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var invalidUser = new CreateUserRequest
        {
            Username = "invalid_user",
            Password = "WrongP@ssw0rd!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users/token", invalidUser);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

