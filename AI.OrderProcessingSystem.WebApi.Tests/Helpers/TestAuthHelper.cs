using System.Net.Http.Headers;
using System.Net.Http.Json;
using AI.OrderProcessingSystem.Common.DTOs.Auth;

namespace AI.OrderProcessingSystem.WebApi.Tests.Helpers;

public static class TestAuthHelper
{
    public static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@orderprocessing.local",
            Password = "Admin@12345"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return loginResponse!.Token;
    }

    public static void SetAuthToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await GetAdminTokenAsync(client);
        SetAuthToken(client, token);
    }
}
