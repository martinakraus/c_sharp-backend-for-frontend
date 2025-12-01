namespace BackendForFrontend.Services;

public interface ISessionService
{
    Task StoreAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
