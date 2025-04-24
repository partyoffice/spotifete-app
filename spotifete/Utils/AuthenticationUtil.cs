using Org.OpenAPITools.Api;

namespace spotifete.Utils;

public static class AuthenticationUtil
{
    public const string SessionId = "sessionId";
    public const string Username = "username";

    public static async Task<bool> IsUserAuthenticated(IAuthenticationApi authenticationApi)
    {
        var sessionId = await SecureStorage.Default.GetAsync(SessionId);
        if (sessionId == null) return false;
        var isSessionAuthenticated = await authenticationApi.IsSessionAuthenticatedAsync(sessionId);
        return isSessionAuthenticated.IsOk;
    }
}