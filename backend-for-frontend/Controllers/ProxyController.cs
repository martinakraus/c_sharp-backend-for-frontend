using BackendForFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendForFrontend.Controllers;

[ApiController]
[Route("api")]
public class ProxyController : ControllerBase
{
    private readonly IApiProxyService _proxyService;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        IApiProxyService proxyService,
        ILogger<ProxyController> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    [HttpPut("{**path}")]
    [HttpDelete("{**path}")]
    [HttpPatch("{**path}")]
    public async Task<IActionResult> ProxyRequest(string path)
    {
        // Extract access token from cookie
        var accessToken = Request.Cookies["access_token"];

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Proxy request without access token");
            return Unauthorized(new { error = "Not authenticated" });
        }

        try
        {
            var (response, newAccessToken) = await _proxyService.ForwardRequestAsync(Request, path, accessToken);

            // If token was refreshed, update the cookie
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(30)
                });
                _logger.LogInformation("Access token refreshed and updated in cookie");
            }

            // Copy response headers (skip problematic ones)
            var headersToSkip = new[] { "Content-Length", "Transfer-Encoding", "Content-Encoding" };
            
            foreach (var header in response.Headers)
            {
                if (!headersToSkip.Contains(header.Key))
                {
                    Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            foreach (var header in response.Content.Headers)
            {
                if (!headersToSkip.Contains(header.Key))
                {
                    Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Return response with appropriate status code
            var content = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            
            _logger.LogInformation("API Response - Status: {StatusCode}, ContentType: {ContentType}, ContentLength: {Length}", 
                statusCode, contentType, content?.Length ?? 0);
            _logger.LogInformation("API Response Content: {Content}", content);
            
            return new ContentResult
            {
                Content = content,
                ContentType = contentType,
                StatusCode = statusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in proxy request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
