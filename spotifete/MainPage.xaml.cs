using System.Collections.ObjectModel;
using System.Windows.Input;
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

    private bool _isMySessionsRefreshing;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        _listeningSession = ApiHelper.Instance().ListeningSessionApi;
        _authentication = ApiHelper.Instance().AuthenticationApi;
        _userApi = ApiHelper.Instance().UserApi;
        UpdateOwnListeningSessions();
        CheckIfHasUsername();
    }

    public bool IsMySessionsRefreshing
    {
        get => _isMySessionsRefreshing;
        set
        {
            if (_isMySessionsRefreshing == value) return;
            _isMySessionsRefreshing = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<SlimListeningSession> MyListenSessions { get; } = [];

    public ICommand EnterUsernameCommand => new Command(SetCorrectUsername);
    public ICommand RefreshMySessionsCommand => new Command(RefreshMySessionList);

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        UpdateOwnListeningSessions();
    }

    private void RefreshMySessionList()
    {
        UpdateOwnListeningSessions();
        IsMySessionsRefreshing = false;
    }

    private async void UpdateOwnListeningSessions()
    {
        var username = await SecureStorage.Default.GetAsync(AuthenticationUtil.Username);
        if (username != null) UserNameLabel.Text = username;
        var isAuthenticated = await AuthenticationUtil.IsUserAuthenticated(_authentication);
        SetLoginButtonText(isAuthenticated);
        MySessionsLabel.IsVisible = isAuthenticated;
        if (!isAuthenticated) return;
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
        if (sessionId == null) return;
        var currentUserResponse = await _userApi.GetCurrentUserAsync(sessionId);
        var currentUser = currentUserResponse.Ok();
        if (!currentUserResponse.IsOk || currentUser?.ListeningSessions == null) return;
        MyListenSessions.Clear();
        currentUser.ListeningSessions.ForEach(session =>
        {
            if (session is { Title: not null, JoinId: not null })
                MyListenSessions.Add(new SlimListeningSession(session.Title, session.JoinId));
        });
    }

    private async void CheckIfHasUsername()
    {
        var isAuthenticated = await AuthenticationUtil.IsUserAuthenticated(_authentication);
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
        var currentUsername = await SecureStorage.Default.GetAsync(AuthenticationUtil.Username);
        switch (isAuthenticated)
        {
            case false when string.IsNullOrWhiteSpace(currentUsername):
                SetCorrectUsername();
                break;
            case true when string.IsNullOrWhiteSpace(currentUsername):
            {
                if (sessionId == null)
                {
                    SetCorrectUsername();
                    return;
                }

                var currentUserResponse = await _userApi.GetCurrentUserAsync(sessionId);
                var spotifyDisplayName = currentUserResponse.Ok()?.SpotifyDisplayName;
                if (spotifyDisplayName == null)
                {
                    SetCorrectUsername();
                    return;
                }

                await SecureStorage.Default.SetAsync(AuthenticationUtil.Username, spotifyDisplayName);
                UserNameLabel.Text = spotifyDisplayName;
                break;
            }
            default:
            {
                UserNameLabel.Text = currentUsername;
                break;
            }
        }
    } 

    private async void SetCorrectUsername()
    {
        var enteredUsername = await DisplayPromptAsync("Username", "Enter your username", "OK", null, null, 30);
        if (string.IsNullOrWhiteSpace(enteredUsername))
        {
            CheckIfHasUsername();
            return;
        }
        await SecureStorage.Default.SetAsync(AuthenticationUtil.Username, enteredUsername);
        UserNameLabel.Text = enteredUsername;
    }

    private async void OnEnteredSessionID(object sender, EventArgs e)
    {
        CheckIfHasUsername();
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
        CheckIfHasUsername();
        var isAuthenticated = await AuthenticationUtil.IsUserAuthenticated(_authentication);
        if (!isAuthenticated) await CreateNewSession();
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
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
        if (!await AuthenticationUtil.IsUserAuthenticated(_authentication))
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

    private async Task SaveSessionIdToStorage(NewAuthenticationSessionResponse newSessionResponse)
    {
        var spotifeteSessionId = newSessionResponse.SpotifeteSessionId;
        if (spotifeteSessionId == null) return;
        await SecureStorage.Default.SetAsync(AuthenticationUtil.SessionId, spotifeteSessionId);
        var currentUserResponse = await _userApi.GetCurrentUserAsync(spotifeteSessionId);
        var spotifyDisplayName = currentUserResponse.Ok()?.SpotifyDisplayName;
        if (spotifyDisplayName != null)
        {
            await SecureStorage.Default.SetAsync(AuthenticationUtil.Username,
                spotifyDisplayName);
            UserNameLabel.Text = spotifyDisplayName;
        }
    }

    private static void DeleteSessionIdFromStorage()
    {
        SecureStorage.Default.Remove(AuthenticationUtil.SessionId);
    }

    private async void OnSessionClicked(object sender, ItemTappedEventArgs e)
    {
        CheckIfHasUsername();
        if (e.Item is not SlimListeningSession selectedListeningSession) return;
        await RedirectToCurrentSession(selectedListeningSession.JoinId);
    }
}