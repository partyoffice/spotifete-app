using System.Collections.ObjectModel;
using Org.OpenAPITools.Api;
using Org.OpenAPITools.Model;
using spotifete.Models;
using spotifete.Sessions;
using spotifete.SpotifeteApi;
using spotifete.Utils;

namespace spotifete;

public partial class MainPage : ContentPage
{
    private readonly IAuthenticationApi _authentication;
    private readonly IListeningSessionApi _listeningSession;
    private readonly IUserApi _userApi;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        _listeningSession = ApiHelper.Instance().ListeningSessionApi;
        _authentication = ApiHelper.Instance().AuthenticationApi;
        _userApi = ApiHelper.Instance().UserApi;
        UpdateOwnListeningSessions();
    }

    public ObservableCollection<SlimListeningSession> MyListenSessions { get; } = new();

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        UpdateOwnListeningSessions();
    }

    private async void UpdateOwnListeningSessions()
    {
        var isAuthenticated = await AuthenticationUtil.isUserAuthenticated(_authentication);
        SetLoginButtonText(isAuthenticated);
        if (!isAuthenticated) return;
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SESSION_ID);
        if (sessionId == null) return;
        var currentUserResponse = await _userApi.GetCurrentUserAsync(sessionId);
        var currentUser = currentUserResponse.Ok();
        if (!currentUserResponse.IsOk || currentUser == null || currentUser.ListeningSessions == null) return;
        MyListenSessions.Clear();
        currentUser.ListeningSessions.ForEach(session =>
            MyListenSessions.Add(new SlimListeningSession(session.Title, session.JoinId)));
    }

    private async void OnEnteredSessionID(object sender, EventArgs e)
    {
        await RedirectToCurrentSession(SessionId.Text);
    }

    private async Task RedirectToCurrentSession(string sessionId)
    {
        SessionId.Text = "";
        var getListeningSessionApiResponse = await _listeningSession.GetListeningSessionAsync(sessionId);
        if (getListeningSessionApiResponse.IsOk)
        {
            var fullListeningSession = getListeningSessionApiResponse.Ok();
            if (fullListeningSession != null)
                await Navigation.PushAsync(new CurrentSession(fullListeningSession, _listeningSession, _authentication,
                    _userApi));
        }
    }

    private async void OnCompletedSessionTitle(object sender, EventArgs e)
    {
        var isAuthenticated = await AuthenticationUtil.isUserAuthenticated(_authentication);
        if (!isAuthenticated) await CreateNewSession();
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SESSION_ID);
        var newSessionRequest = new NewListeningSessionRequest(sessionId, CreateSession.Text);
        CreateSession.Text = "";
        var newSession = await _listeningSession.CreateNewListeningSessionAsync(newSessionRequest);
        var newSessionResponse = newSession.Ok();
        if (!newSession.IsOk || newSessionResponse == null) return;
        if (newSessionResponse.JoinId == null) return;
        await RedirectToCurrentSession(newSessionResponse.JoinId);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (!await AuthenticationUtil.isUserAuthenticated(_authentication))
            await CreateNewSession();
        else
            LogoutFromSpotify();
    }

    private void LogoutFromSpotify()
    {
        DeleteSessionIdFromStorage();
        MyListenSessions.Clear();
        UpdateOwnListeningSessions();
    }

    private async Task CreateNewSession()
    {
        var apiResponse = await _authentication.NewAuthenticationSessionAsync("/app");
        var newSessionResponse = apiResponse.Ok();
        if (!apiResponse.IsOk || newSessionResponse == null) return;
        await SaveSessionIdToStorage(newSessionResponse);
        await OpenBrowserForSpotify(newSessionResponse);
    }

    private void SetLoginButtonText(bool isAuthenticated)
    {
        Login.Text = isAuthenticated ? "Logout from Spotify" : "Login to Spotify";
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

    private void DeleteSessionIdFromStorage()
    {
        SecureStorage.Default.Remove(AuthenticationUtil.SESSION_ID);
    }

    private async void OnSessionClicked(object sender, ItemTappedEventArgs e)
    {
        var selectedListeningSession = e.Item as SlimListeningSession;
        if (selectedListeningSession == null) return;
        await RedirectToCurrentSession(selectedListeningSession.JoinId);
    }
}