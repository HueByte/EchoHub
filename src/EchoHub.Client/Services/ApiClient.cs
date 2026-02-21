using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;

namespace EchoHub.Client.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt;

    public string? Token => _accessToken;
    public string? RefreshToken => _refreshToken;
    public string BaseUrl { get; }

    public event Action? OnTokensRefreshed;

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

        SetTokens(result);
        return result;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest(username, password);
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned empty response.");

        SetTokens(result);
        return result;
    }

    public async Task RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
            throw new InvalidOperationException("No refresh token available.");

        var request = new RefreshRequest(_refreshToken);
        var response = await _http.PostAsJsonAsync("/api/auth/refresh", request);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Token refresh returned empty response.");

        SetTokens(result);
    }

    public async Task<LoginResponse> LoginWithRefreshTokenAsync(string refreshToken)
    {
        var request = new RefreshRequest(refreshToken);
        var response = await _http.PostAsJsonAsync("/api/auth/refresh", request);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Token refresh returned empty response.");

        SetTokens(result);
        return result;
    }

    public async Task LogoutAsync()
    {
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            try
            {
                var request = new RefreshRequest(_refreshToken);
                await _http.PostAsJsonAsync("/api/auth/logout", request);
            }
            catch
            {
                // Best-effort logout
            }
        }

        _accessToken = null;
        _refreshToken = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Returns a valid access token, refreshing if expired.
    /// Used by EchoHubConnection for SignalR token provider.
    /// </summary>
    public async Task<string?> GetValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        // Refresh if token expires within 60 seconds
        if (DateTimeOffset.UtcNow >= _expiresAt.AddSeconds(-60) && !string.IsNullOrEmpty(_refreshToken))
        {
            try
            {
                await RefreshTokenAsync();
            }
            catch
            {
                // Return current token and let the caller handle auth failure
            }
        }

        return _accessToken;
    }

    public async Task<List<ChannelDto>> GetChannelsAsync()
    {
        EnsureAuthenticated();
        var response = await AuthenticatedGetAsync("/api/channels");
        await EnsureSuccessAsync(response);
        var paginated = await response.Content.ReadFromJsonAsync<PaginatedResponse<ChannelDto>>();
        return paginated?.Items ?? [];
    }

    public async Task<ServerStatusDto?> GetServerInfoAsync()
    {
        var info = await _http.GetFromJsonAsync<ServerStatusDto>("/api/server/info");
        return info;
    }

    public async Task<string> GetEncryptionKeyAsync()
    {
        EnsureAuthenticated();
        var response = await AuthenticatedGetAsync("/api/server/encryption-key");
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<EncryptionKeyResponse>()
            ?? throw new InvalidOperationException("Server returned empty encryption key response.");
        return result.Key;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedGetAsync($"/api/users/{Uri.EscapeDataString(username)}/profile");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UserProfileDto>();
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PutAsJsonAsync("/api/users/profile", request));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UserProfileDto>();
    }

    public async Task<string?> UploadAvatarAsync(Stream imageStream, string fileName)
    {
        EnsureAuthenticated();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsync("/api/users/avatar", content));
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<AvatarUploadResponse>();
        return result?.AvatarAscii;
    }

    public async Task<MessageDto?> UploadFileAsync(string channelName, Stream fileStream, string fileName, string? size = null)
    {
        EnsureAuthenticated();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        var sizeQuery = size is not null ? $"?size={size}" : "";
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsync($"/api/channels/{Uri.EscapeDataString(channelName)}/upload{sizeQuery}", content));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<MessageDto>();
    }

    public async Task<MessageDto?> SendUrlAsync(string channelName, string url, string? size = null)
    {
        EnsureAuthenticated();
        var request = new SendUrlRequest(url);
        var sizeQuery = size is not null ? $"?size={size}" : "";
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/channels/{Uri.EscapeDataString(channelName)}/send-url{sizeQuery}", request));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<MessageDto>();
    }

    public async Task<string> DownloadFileToTempAsync(string relativeUrl, string fileName)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedGetAsync(relativeUrl);
        await EnsureSuccessAsync(response);

        var tempDir = Path.Combine(Path.GetTempPath(), "EchoHub");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}_{fileName}");

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(tempPath);
        await stream.CopyToAsync(file);

        return tempPath;
    }

    public async Task<ChannelDto?> CreateChannelAsync(string name, string? topic = null, bool isPublic = true)
    {
        EnsureAuthenticated();
        var request = new CreateChannelRequest(name, topic, isPublic);
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync("/api/channels", request));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ChannelDto>();
    }

    public async Task<ChannelDto?> UpdateChannelTopicAsync(string channelName, string? topic)
    {
        EnsureAuthenticated();
        var request = new UpdateTopicRequest(topic);
        var response = await AuthenticatedRequestAsync(() =>
            _http.PutAsJsonAsync($"/api/channels/{Uri.EscapeDataString(channelName)}/topic", request));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ChannelDto>();
    }

    public async Task DeleteChannelAsync(string channelName)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.DeleteAsync($"/api/channels/{Uri.EscapeDataString(channelName)}"));
        await EnsureSuccessAsync(response);
    }

    // ── Moderation ────────────────────────────────────────────────────────

    public async Task AssignRoleAsync(string username, ServerRole role)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync("/api/moderation/role", new AssignRoleRequest(username, role)));
        await EnsureSuccessAsync(response);
    }

    public async Task KickUserAsync(string username, string? reason = null)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/moderation/kick/{Uri.EscapeDataString(username)}", new KickRequest(reason)));
        await EnsureSuccessAsync(response);
    }

    public async Task BanUserAsync(string username, string? reason = null)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/moderation/ban/{Uri.EscapeDataString(username)}", new BanRequest(reason)));
        await EnsureSuccessAsync(response);
    }

    public async Task UnbanUserAsync(string username)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/moderation/unban/{Uri.EscapeDataString(username)}", new { }));
        await EnsureSuccessAsync(response);
    }

    public async Task MuteUserAsync(string username, int? durationMinutes = null, string? reason = null)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/moderation/mute/{Uri.EscapeDataString(username)}", new MuteRequest(reason, durationMinutes)));
        await EnsureSuccessAsync(response);
    }

    public async Task UnmuteUserAsync(string username)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.PostAsJsonAsync($"/api/moderation/unmute/{Uri.EscapeDataString(username)}", new { }));
        await EnsureSuccessAsync(response);
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.DeleteAsync($"/api/moderation/messages/{messageId}"));
        await EnsureSuccessAsync(response);
    }

    public async Task NukeChannelAsync(string channelName)
    {
        EnsureAuthenticated();
        var response = await AuthenticatedRequestAsync(() =>
            _http.DeleteAsync($"/api/moderation/channels/{Uri.EscapeDataString(channelName)}/nuke"));
        await EnsureSuccessAsync(response);
    }

    private void SetTokens(LoginResponse result)
    {
        _accessToken = result.Token;
        _refreshToken = result.RefreshToken;
        _expiresAt = result.ExpiresAt;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        OnTokensRefreshed?.Invoke();
    }

    /// <summary>
    /// Performs a GET request with automatic token refresh on 401.
    /// </summary>
    private async Task<HttpResponseMessage> AuthenticatedGetAsync(string url)
    {
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_refreshToken))
        {
            try
            {
                await RefreshTokenAsync();
                response = await _http.GetAsync(url);
            }
            catch
            {
                // Refresh failed, return original 401
            }
        }

        return response;
    }

    /// <summary>
    /// Performs a request with automatic token refresh on 401.
    /// </summary>
    private async Task<HttpResponseMessage> AuthenticatedRequestAsync(Func<Task<HttpResponseMessage>> requestFactory)
    {
        var response = await requestFactory();

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_refreshToken))
        {
            try
            {
                await RefreshTokenAsync();
                response = await requestFactory();
            }
            catch
            {
                // Refresh failed, return original 401
            }
        }

        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorMessage = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
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
        if (string.IsNullOrEmpty(_accessToken))
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
