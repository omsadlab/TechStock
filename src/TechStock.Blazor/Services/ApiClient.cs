using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace TechStock.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;

    public ApiClient(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _storage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<T>(url);
    }

    public async Task<T?> PostAsync<T>(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ReadErrorAsync(response));
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task PostAsync(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ReadErrorAsync(response));
    }

    public async Task<T?> PutAsync<T>(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ReadErrorAsync(response));
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task PutAsync(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ReadErrorAsync(response));
    }

    public async Task DeleteAsync(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync(url);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await ReadErrorAsync(response));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err) && err.GetString() is { } e)
                    return e;
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.GetString() is { } m)
                    return m;
            }
        }
        catch { }
        return $"{(int)response.StatusCode} {response.ReasonPhrase}";
    }

    public async Task<byte[]> GetBytesAsync(string url)
    {
        await SetAuthHeaderAsync();
        return await _http.GetByteArrayAsync(url);
    }
}
