using System.Collections.ObjectModel;
using System.Windows.Input;
using Org.OpenAPITools.Api;
using Org.OpenAPITools.Model;
using spotifete.Utils;
using Timer = System.Timers.Timer;

namespace spotifete.Sessions;

public partial class CurrentSession : ContentPage
{
    private const double RefreshInterval = 5000;
    private const int SearchedTracksLimit = 25;
    private readonly IAuthenticationApi _authentication;
    private readonly IListeningSessionApi _listeningSession;
    private readonly string _savedJoinId;
    private readonly IUserApi _userApi;
    private DateTime _lastSavedDateTime = DateTime.Now;
    private Timer _timer = new();

    public CurrentSession(FullListeningSession fullListeningSession,
        IListeningSessionApi listeningSession, IAuthenticationApi authentication, IUserApi userApi)
    {
        InitializeComponent();
        BindingContext = this;

        _listeningSession = listeningSession;
        _authentication = authentication;
        _userApi = userApi;
        _savedJoinId = fullListeningSession.JoinId ?? " ";
        SessionTitle.Text = fullListeningSession.Title ?? " ";
        SessionCode.Text = "Session code: " + _savedJoinId;

        Init();

        UpdateQueueInformation();
        SetTimer();
    }

    public ObservableCollection<SongRequest> CurrentQueue { get; private set; } = [];

    public ObservableCollection<TrackMetaData> SearchedTracks { get; } = [];

    public ICommand SessionCodeCopyCommand => new Command(OnClickedSessionCode);

    private async void Init()
    {
        var getListeningSessionApiResponse = await _listeningSession.GetListeningSessionAsync(_savedJoinId);
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SESSION_ID);
        if (sessionId == null) return;
        var getCurrentUser = await _userApi.GetCurrentUserAsync(sessionId);
        if (!getListeningSessionApiResponse.IsOk && !getCurrentUser.IsOk)
        {
            DeletionButton.IsVisible = false;
            return;
        }

        DeletionButton.IsVisible =
            getListeningSessionApiResponse.Ok()?.Owner?.SpotifyId == getCurrentUser.Ok()?.SpotifyId;
    }

    private async void UpdateQueueInformation()
    {
        var queueLastUpdatedAsync = await _listeningSession.GetSessionQueueAsync(_savedJoinId);
        if (!queueLastUpdatedAsync.IsOk) return;
        CurrentQueue.Clear();
        var songRequests = queueLastUpdatedAsync.Ok()?.Queue;
        if (songRequests == null) return;
        foreach (var item in songRequests)
            CurrentQueue.Add(item);
    }

    private async void CheckForNecessaryUpdate()
    {
        var lastUpdated = await _listeningSession.QueueLastUpdatedAsync(_savedJoinId);

        var lastUpdatedDateTime = lastUpdated.Ok()!.QueueLastUpdated;
        if (lastUpdated.IsNotFound)
        {
            _timer.Stop();
            MainThread.BeginInvokeOnMainThread(BackToHomeScreen);
        }
        else
        {
            if (_lastSavedDateTime.CompareTo(lastUpdatedDateTime) == 0) return;
            if (lastUpdatedDateTime != null) _lastSavedDateTime = lastUpdatedDateTime.Value;
            UpdateQueueInformation();
        }
    }

    private async void SearchForSongs(object sender, EventArgs e)
    {
        var currentSearchRequest = SearchedSong.Text;
        if (!(currentSearchRequest.Length > 1))
        {
            SearchedTracks.Clear();
            return;
        }

        var allSongsOfSearch =
            await _listeningSession.SearchTrackAsync(_savedJoinId, currentSearchRequest, SearchedTracksLimit);
        if (!allSongsOfSearch.IsOk) return;
        SearchedTracks.Clear();
        var trackMetaDatas = allSongsOfSearch.Ok()?.Tracks;
        if (trackMetaDatas == null) return;
        foreach (var item in trackMetaDatas)
            SearchedTracks.Add(item);
    }

    private async void AddSongToQueue(object sender, SelectedItemChangedEventArgs e)
    {
        var selectedTrack = e.SelectedItem as TrackMetaData;
        var queue = await _listeningSession.RequestTrackAsync(_savedJoinId,
            new RequestTrackRequest(" ", selectedTrack?.SpotifyTrackId));
        if (!queue.IsNoContent) return;

        SearchedSong.Text = "";
        SearchedTracks.Clear();
        UpdateQueueInformation();
    }

    private async void DeleteCurrentSession()
    {
        var isAuthenticated = await AuthenticationUtil.isUserAuthenticated(_authentication);
        if (!isAuthenticated) return;
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SESSION_ID);
        await _listeningSession.CloseListeningSessionAsync(_savedJoinId, new AuthenticatedRequest(sessionId));
    }

    private void SetTimer()
    {
        _timer = new Timer(RefreshInterval);
        _timer.Elapsed += (_, _) => CheckForNecessaryUpdate();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private async void OnClickedSessionCode()
    {
        await Clipboard.Default.SetTextAsync(_savedJoinId);
    }

    private void OnClickedDeleteSession(object sender, EventArgs e)
    {
        DeleteCurrentSession();
        BackToHomeScreen();
    }

    private void BackToHomeScreen()
    {
        _timer.Stop();
        Navigation.PopAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _timer.Stop();
        return base.OnBackButtonPressed();
    }
}