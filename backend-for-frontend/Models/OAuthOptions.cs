namespace BackendForFrontend.Models;

public class OAuthOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ExternalAuthority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string RedirectUri { get; set; } = string.Empty;
    public string PostLogoutRedirectUri { get; set; } = string.Empty;
}
