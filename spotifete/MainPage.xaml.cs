using Org.OpenAPITools.Api;
using Org.OpenAPITools.Model;
using spotifete.Sessions;
using spotifete.SpotifeteApi;
using spotifete.Utils;

namespace spotifete;

public partial class MainPage : ContentPage
{
    private readonly IAuthenticationApi _authentication;
    private readonly IListeningSessionApi _listeningSession;

    public MainPage()
    {
        InitializeComponent();
        _listeningSession = ApiHelper.Instance().ListeningSessionApi;
        _authentication = ApiHelper.Instance().AuthenticationApi;
    }

    private async void OnEnteredSessionID(object sender, EventArgs e)
    {
        await RedirectToCurrentSession(SessionId.Text);
    }

    private async Task RedirectToCurrentSession(string sessionId)
    {
        var getListeningSessionApiResponse = await _listeningSession.GetListeningSessionAsync(sessionId);
        if (getListeningSessionApiResponse.IsOk)
        {
            var fullListeningSession = getListeningSessionApiResponse.Ok();
            await Navigation.PushAsync(new CurrentSession(fullListeningSession, _listeningSession));
        }
    }

    private async void OnCompletedSessionTitle(object sender, EventArgs e)
    {
        var isAuthenticated = await AuthenticationUtil.isUserAuthenticated(_authentication);
        if (!isAuthenticated) await CreateNewSession();
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SESSION_ID);
        var newSessionRequest = new NewListeningSessionRequest(sessionId, CreateSession.Text);
        var newSession = await _listeningSession.CreateNewListeningSessionAsync(newSessionRequest);
        var newSessionResponse = newSession.Ok();
        if (!newSession.IsOk || newSessionResponse == null) return;
        if (newSessionResponse.JoinId == null) return;
        await RedirectToCurrentSession(newSessionResponse.JoinId);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await CreateNewSession();
    }

    private async Task CreateNewSession()
    {
        var isAuthenticated = await AuthenticationUtil.isUserAuthenticated(_authentication);
        if (isAuthenticated) return;
        var apiResponse = await _authentication.NewAuthenticationSessionAsync("/app");
        var newSessionResponse = apiResponse.Ok();
        if (!apiResponse.IsOk || newSessionResponse == null) return;
        await SaveSessionIdToStorage(newSessionResponse);
        await OpenBrowserForSpotify(newSessionResponse);
    }

    private static async Task OpenBrowserForSpotify(NewAuthenticationSessionResponse newSessionResponse)
    {
        if (newSessionResponse.SpotifyAuthenticationUrl == null) return;
        var uri = new Uri(newSessionResponse.SpotifyAuthenticationUrl);
        var browserLaunchOptions = new WebAuthenticatorOptions
        {
            Url = uri,
            CallbackUrl = new Uri("https://spotifete.nikos410.de/app/android"),
            PrefersEphemeralWebBrowserSession = true
        };
        await WebAuthenticator.Default.AuthenticateAsync(browserLaunchOptions);
    }

    private static async Task SaveSessionIdToStorage(NewAuthenticationSessionResponse newSessionResponse)
    {
        var spotifeteSessionId = newSessionResponse.SpotifeteSessionId;
        if (spotifeteSessionId == null) return;
        await SecureStorage.Default.SetAsync(AuthenticationUtil.SESSION_ID, spotifeteSessionId);
    }
}