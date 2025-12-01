namespace BackendForFrontend.Services;

public interface IOAuthService
{
    Task<string> BuildAuthorizationUrlAsync();
    Task<string?> ExchangeCodeForTokenAsync(string code, string codeVerifier);
    Task<string?> RefreshTokenAsync(string refreshToken);
    Task<string> LogoutAsync(string? idToken = null);
}
