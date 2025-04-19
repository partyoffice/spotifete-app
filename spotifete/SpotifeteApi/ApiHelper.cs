using Microsoft.Extensions.Hosting;
using Org.OpenAPITools.Api;
using Org.OpenAPITools.Extensions;

namespace spotifete.SpotifeteApi;

public class ApiHelper
{
    private static ApiHelper _instance;
    private static readonly object _lock = new();

    private static IHost _host;
    private static IAuthenticationApi _authenticationApi;
    private static IListeningSessionApi _listeningSessionApi;
    private static IUserApi _userApi;

    private ApiHelper()
    {
        _host = Host.CreateDefaultBuilder().ConfigureApi().Build();
        _authenticationApi = _host.Services.GetRequiredService<IAuthenticationApi>();
        _listeningSessionApi = _host.Services.GetRequiredService<IListeningSessionApi>();
        _userApi = _host.Services.GetRequiredService<IUserApi>();
    }

    public IAuthenticationApi AuthenticationApi => _authenticationApi;
    public IListeningSessionApi ListeningSessionApi => _listeningSessionApi;
    public IUserApi UserApi => _userApi;

    public static ApiHelper Instance()
    {
        lock (_lock)
        {
            if (_instance == null) _instance = new ApiHelper();
        }

        return _instance;
    }
}