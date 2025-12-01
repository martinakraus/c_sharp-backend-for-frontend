namespace BackendForFrontend.Services;

public class SessionService : ISessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task StoreAsync(string key, string value)
    {
        _httpContextAccessor.HttpContext?.Session.SetString(key, value);
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        var value = _httpContextAccessor.HttpContext?.Session.GetString(key);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key)
    {
        _httpContextAccessor.HttpContext?.Session.Remove(key);
        return Task.CompletedTask;
    }
}
