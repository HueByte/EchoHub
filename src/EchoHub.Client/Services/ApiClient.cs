using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EchoHub.Core.DTOs;

namespace EchoHub.Client.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _token;

    public string? Token => _token;
    public string BaseUrl { get; }

    public ApiClient(string baseUrl)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    public async Task<LoginResponse> RegisterAsync(string username, string password, string? displayName = null)
    {
        var request = new RegisterRequest(username, password, displayName);
        var response = await _http.PostAsJsonAsync("/api/auth/register", request);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Registration returned empty response.");

        SetToken(result.Token);
        return result;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest(username, password);
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned empty response.");

        SetToken(result.Token);
        return result;
    }

    public async Task<List<ChannelDto>> GetChannelsAsync()
    {
        EnsureAuthenticated();
        var channels = await _http.GetFromJsonAsync<List<ChannelDto>>("/api/channels");
        return channels ?? [];
    }

    public async Task<ServerStatusDto?> GetServerInfoAsync()
    {
        var info = await _http.GetFromJsonAsync<ServerStatusDto>("/api/server/info");
        return info;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        EnsureAuthenticated();
        var profile = await _http.GetFromJsonAsync<UserProfileDto>($"/api/users/{Uri.EscapeDataString(username)}/profile");
        return profile;
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        EnsureAuthenticated();
        var response = await _http.PutAsJsonAsync("/api/users/profile", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileDto>();
    }

    public async Task<string?> UploadAvatarAsync(Stream imageStream, string fileName)
    {
        EnsureAuthenticated();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync("/api/users/avatar", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>();
        return result?.AsciiArt;
    }

    public async Task<MessageDto?> UploadFileAsync(string channelName, Stream fileStream, string fileName)
    {
        EnsureAuthenticated();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync($"/api/channels/{Uri.EscapeDataString(channelName)}/upload", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>();
    }

    private void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        // Try to extract a meaningful error message from the response body
        var errorMessage = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                // Try to parse {"error": "..."} format
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var errorProp) ||
                    doc.RootElement.TryGetProperty("Error", out errorProp))
                {
                    errorMessage = errorProp.GetString() ?? errorMessage;
                }
                else
                {
                    errorMessage = body;
                }
            }
        }
        catch
        {
            // If we can't parse the body, use the status code message
        }

        throw new HttpRequestException(errorMessage);
    }

    private void EnsureAuthenticated()
    {
        if (string.IsNullOrEmpty(_token))
            throw new InvalidOperationException("Not authenticated. Call LoginAsync or RegisterAsync first.");
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

internal record AvatarUploadResponse(string AsciiArt);
