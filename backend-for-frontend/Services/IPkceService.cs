namespace BackendForFrontend.Services;

public interface IPkceService
{
    string GenerateCodeVerifier();
    string GenerateCodeChallenge(string codeVerifier);
    string GenerateState();
}
