using System.IdentityModel.Tokens.Jwt;
using BackendForFrontend.Models;
using Microsoft.Extensions.Options;

namespace BackendForFrontend.Services;

public interface IApiProxyService
{
    Task<(HttpResponseMessage Response, string? NewAccessToken)> ForwardRequestAsync(HttpRequest request, string path, string? accessToken);
}

public class ApiProxyService : IApiProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ApiProxyOptions _options;
    private readonly ILogger<ApiProxyService> _logger;
    private readonly IOAuthService _oauthService;
    private readonly ISessionService _sessionService;

    public ApiProxyService(
        IHttpClientFactory httpClientFactory,
        IOptions<ApiProxyOptions> options,
        ILogger<ApiProxyService> logger,
        IOAuthService oauthService,
        ISessionService sessionService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _options = options.Value;
        _logger = logger;
        _oauthService = oauthService;
        _sessionService = sessionService;
    }

    public async Task<(HttpResponseMessage Response, string? NewAccessToken)> ForwardRequestAsync(HttpRequest request, string path, string? accessToken)
    {
        // Validate token expiration and refresh if needed
        var validAccessToken = await EnsureValidAccessTokenAsync(accessToken);
        
        var targetUrl = $"{_options.ApiBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        
        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(request.Method),
            RequestUri = new Uri(targetUrl)
        };

        // Add Authorization header with access token
        if (!string.IsNullOrEmpty(validAccessToken))
        {
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validAccessToken);
        }

        // Copy relevant headers
        foreach (var header in request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Copy body for POST, PUT, PATCH
        if (request.ContentLength > 0)
        {
            var streamContent = new StreamContent(request.Body);
            if (request.ContentType != null)
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
            }
            requestMessage.Content = streamContent;
        }

        _logger.LogInformation("Forwarding {Method} request to {Url}", request.Method, targetUrl);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            return (response, validAccessToken != accessToken ? validAccessToken : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding request to API");
            throw;
        }
    }

    private async Task<string?> EnsureValidAccessTokenAsync(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No access token provided");
            return accessToken;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            var expirationTime = jwtToken.ValidTo;
            
            // Check if token expires in the next 30 seconds (proactive refresh)
            var isExpiringSoon = expirationTime <= DateTime.UtcNow.AddSeconds(30);
            
            if (isExpiringSoon)
            {
                _logger.LogInformation("Access token is expiring soon (exp: {ExpirationTime}), refreshing...", expirationTime);
                
                // Get refresh token from session
                var refreshToken = await _sessionService.GetAsync("refresh_token");
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("No refresh token found in session");
                    return accessToken;
                }
                
                // Refresh the token
                var newAccessToken = await _oauthService.RefreshTokenAsync(refreshToken);
                
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    _logger.LogInformation("Access token successfully refreshed");
                    return newAccessToken;
                }
                else
                {
                    _logger.LogWarning("Token refresh failed, using existing token");
                    return accessToken;
                }
            }
            
            _logger.LogDebug("Access token is still valid (exp: {ExpirationTime})", expirationTime);
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access token");
            return accessToken;
        }
    }
}
