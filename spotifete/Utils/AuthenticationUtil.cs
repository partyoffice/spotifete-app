using Org.OpenAPITools.Api;

namespace spotifete.Utils;

public static class AuthenticationUtil
{
    public const string SESSION_ID = "sessionId";
    public const string USERNAME = "username";

    public static async Task<bool> isUserAuthenticated(IAuthenticationApi authenticationApi)
    {
        var sessionId = await SecureStorage.Default.GetAsync(SESSION_ID);
        if (sessionId == null) return false;
        var isSessionAuthenticated = await authenticationApi.IsSessionAuthenticatedAsync(sessionId);
        return isSessionAuthenticated.IsOk;
    }
}