using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace TechStock.Blazor.Services;

public class BlazorAuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly AppAuthStateProvider _authState;

    public BlazorAuthService(HttpClient http, ILocalStorageService storage, AuthenticationStateProvider authState)
    {
        _http = http;
        _storage = storage;
        _authState = (AppAuthStateProvider)authState;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result == null) return false;

        await _storage.SetItemAsync("authToken", result.Token);
        await _storage.SetItemAsync("refreshToken", result.RefreshToken);
        _authState.NotifyAuthChanged();
        return true;
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await _storage.GetItemAsync<string>("refreshToken");
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try { await _http.PostAsJsonAsync("api/auth/logout", new { refreshToken }); }
            catch { /* best effort */ }
        }

        await _storage.RemoveItemAsync("authToken");
        await _storage.RemoveItemAsync("refreshToken");
        _authState.NotifyAuthChanged();
    }

    private record AuthResponse(string Token, string RefreshToken, object User);
}
