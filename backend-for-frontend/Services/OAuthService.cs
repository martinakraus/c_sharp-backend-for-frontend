using System.Text;
using System.Text.Json;
using BackendForFrontend.Models;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace BackendForFrontend.Services;

public class OAuthService : IOAuthService
{
    private readonly OAuthOptions _options;
    private readonly IPkceService _pkceService;
    private readonly ISessionService _sessionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(
        IOptions<OAuthOptions> options,
        IPkceService pkceService,
        ISessionService sessionService,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthService> logger)
    {
        _options = options.Value;
        _pkceService = pkceService;
        _sessionService = sessionService;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<string> BuildAuthorizationUrlAsync()
    {
        var codeVerifier = _pkceService.GenerateCodeVerifier();
        var codeChallenge = _pkceService.GenerateCodeChallenge(codeVerifier);
        var state = _pkceService.GenerateState();

        await _sessionService.StoreAsync("code_verifier", codeVerifier);
        await _sessionService.StoreAsync("oauth_state", state);

        var authUrl = $"{_options.Authority}/protocol/openid-connect/auth" +
                     $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
                     $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                     $"&response_type=code" +
                     $"&scope=openid profile offline_access" +
                     $"&state={Uri.EscapeDataString(state)}" +
                     $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                     $"&code_challenge_method=S256";

        return authUrl;
    }

    public async Task<string?> ExchangeCodeForTokenAsync(string code, string codeVerifier)
    {
        var tokenEndpoint = $"{_options.Authority}/protocol/openid-connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", _options.RedirectUri },
            { "client_id", _options.ClientId },
            { "client_secret", _options.ClientSecret },
            { "code_verifier", codeVerifier }
        };

        var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        var accessToken = tokenResponse.GetProperty("access_token").GetString();
        var refreshToken = tokenResponse.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var idToken = tokenResponse.TryGetProperty("id_token", out var it) ? it.GetString() : null;
        
        _logger.LogInformation("Access Token received: {AccessToken}", accessToken != null ? $"{accessToken}" : "null");
        _logger.LogInformation("Refresh Token received: {RefreshToken}", refreshToken != null ? $"{refreshToken}" : "null");
        _logger.LogInformation("ID Token received: {IdToken}", idToken != null ? $"{idToken}" : "null");
        
        // Store refresh token in session for later use
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _sessionService.StoreAsync("refresh_token", refreshToken);
        }
        
        // Store ID token in session for logout
        if (!string.IsNullOrEmpty(idToken))
        {
            await _sessionService.StoreAsync("id_token", idToken);
        }
        
        return accessToken;
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        var tokenEndpoint = $"{_options.Authority}/protocol/openid-connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _options.ClientId },
            { "client_secret", _options.ClientSecret }
        };

        var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token refresh failed");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        var newAccessToken = tokenResponse.GetProperty("access_token").GetString();
        var newRefreshToken = tokenResponse.TryGetProperty("refresh_token", out var nrt) ? nrt.GetString() : null;
        var newIdToken = tokenResponse.TryGetProperty("id_token", out var nit) ? nit.GetString() : null;
        
        _logger.LogInformation("=== TOKEN REFRESH ===");
        _logger.LogInformation("Access Token abgelaufen - Neues Token Paar geholt");
        _logger.LogInformation("New Access Token received: {AccessToken}", newAccessToken != null ? $"{newAccessToken[..20]}..." : "null");
        _logger.LogInformation("New Refresh Token received: {RefreshToken}", newRefreshToken != null ? $"{newRefreshToken[..20]}..." : "null");
        _logger.LogInformation("New ID Token received: {IdToken}", newIdToken != null ? $"{newIdToken[..20]}..." : "null");
        
        // Token rotation: Store new refresh token (Keycloak rotates by default)
        if (!string.IsNullOrEmpty(newRefreshToken))
        {
            await _sessionService.StoreAsync("refresh_token", newRefreshToken);
        }
        
        // Store new ID token
        if (!string.IsNullOrEmpty(newIdToken))
        {
            await _sessionService.StoreAsync("id_token", newIdToken);
        }
        
        return newAccessToken;
    }

    public Task<string> LogoutAsync(string? idToken = null)
    {
        var logoutEndpoint = $"{_options.Authority}/protocol/openid-connect/logout";
        
        var parameters = new Dictionary<string, string>
        {
            { "client_id", _options.ClientId },
            { "post_logout_redirect_uri", _options.PostLogoutRedirectUri }
        };

        if (!string.IsNullOrEmpty(idToken))
        {
            parameters.Add("id_token_hint", idToken);
        }

        var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var logoutUrl = $"{logoutEndpoint}?{queryString}";
        
        return Task.FromResult(logoutUrl);
    }
}
