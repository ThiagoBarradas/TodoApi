
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi;
using TodoApi.Todos;
using TodoApi.Users;
using Xunit;

public class TodosIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TodosIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> Authenticate()
    {
        var userId = Guid.NewGuid().ToString();
        var newUser = new CreateUserRequest
        {
            Username = $"user_{userId}",
            Password = "P@ssw0rd!"
        };
        await _client.PostAsJsonAsync("/users", newUser);

        var response = await _client.PostAsJsonAsync("/users/token", newUser);
        response.EnsureSuccessStatusCode();
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse.Token;
    }

    [Fact]
    public async Task CreateTodo_ValidTodo_ReturnsCreated()
    {
        // Arrange
        var token = await Authenticate();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newTodo = new CreateTodoRequest
        {
            Title = "Read a book"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/todos", newTodo);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_InvalidTodo_ReturnsBadRequest()
    {
        // Arrange
        var token = await Authenticate();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidTodo = new CreateTodoRequest
        {
            Title = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/todos", invalidTodo);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListTodos_ValidUser_ReturnsTodos()
    {
        // Arrange
        var token = await Authenticate();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/todos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<List<TodoResponse>>());
    }

    [Fact]
    public async Task DeleteTodo_ValidTodo_ReturnsOk()
    {
        // Arrange
        var token = await Authenticate();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newTodo = new CreateTodoRequest
        {
            Title = "Attend meeting"
        };
        var createResponse = await _client.PostAsJsonAsync("/todos", newTodo);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Act
        var response = await _client.DeleteAsync($"/todos/{createdTodo.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_InvalidTodo_ReturnsNotFound()
    {
        // Arrange
        var token = await Authenticate();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/todos/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

