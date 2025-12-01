using BackendForFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendForFrontend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOAuthService oauthService,
        ISessionService sessionService,
        ILogger<AuthController> logger)
    {
        _oauthService = oauthService;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login()
    {
        var authUrl = await _oauthService.BuildAuthorizationUrlAsync();
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return BadRequest("Missing code or state parameter");
        }

        var storedState = await _sessionService.GetAsync("oauth_state");
        if (storedState != state)
        {
            _logger.LogWarning("State mismatch - possible CSRF attack");
            return BadRequest("Invalid state parameter");
        }

        var codeVerifier = await _sessionService.GetAsync("code_verifier");
        if (string.IsNullOrEmpty(codeVerifier))
        {
            return BadRequest("Code verifier not found");
        }

        var accessToken = await _oauthService.ExchangeCodeForTokenAsync(code, codeVerifier);
        if (string.IsNullOrEmpty(accessToken))
        {
            return StatusCode(500, "Failed to exchange code for token");
        }

        // Store access token in secure HTTP-only cookie
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(1)
        });

        // Clean up session
        await _sessionService.RemoveAsync("code_verifier");
        await _sessionService.RemoveAsync("oauth_state");

        return Redirect("http://localhost:4200");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        // Clear all cookies
        Response.Cookies.Delete("access_token");
        
        // Delete all session-related cookies
        foreach (var cookie in Request.Cookies.Keys)
        {
            Response.Cookies.Delete(cookie, new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }
        
        // Clear the entire session (removes code_verifier, oauth_state, etc.)
        HttpContext.Session.Clear();
        
        // Get Keycloak logout URL and redirect browser to it
        // This ensures Keycloak's session cookies are also cleared
        var logoutUrl = await _oauthService.LogoutAsync();
        return Redirect(logoutUrl);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var hasToken = Request.Cookies.ContainsKey("access_token");
        return Ok(new { authenticated = hasToken });
    }
}
