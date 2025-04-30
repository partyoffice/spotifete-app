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
    private bool _arePlaylistSearchResultsVisible;
    private bool _areSongSearchResultsVisible;
    private bool _isMyQueueRefreshing;
    private DateTime _lastSavedDateTime = DateTime.Now;
    private Timer _timer = new();

    public CurrentSession(FullListeningSession fullListeningSession,
        IListeningSessionApi listeningSession, IAuthenticationApi authentication, IUserApi userApi)
    {
        InitializeComponent();

        AreSongSearchResultsVisible = false;
        ArePlaylistSearchResultsVisible = false;

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
    public ObservableCollection<PlaylistMetadata> SearchedPlaylistsList { get; } = [];

    public ICommand SessionCodeCopyCommand => new Command(OnClickedSessionCode);

    public ICommand RefreshMyQueueCommand => new Command(RefreshMyQueueList);

    public bool AreSongSearchResultsVisible
    {
        get => _areSongSearchResultsVisible;
        set
        {
            if (_areSongSearchResultsVisible == value) return;
            _areSongSearchResultsVisible = value;
            OnPropertyChanged();
        }
    }

    public bool ArePlaylistSearchResultsVisible
    {
        get => _arePlaylistSearchResultsVisible;
        set
        {
            if (_arePlaylistSearchResultsVisible == value) return;
            _arePlaylistSearchResultsVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsMyQueueRefreshing
    {
        get => _isMyQueueRefreshing;
        set
        {
            if (_isMyQueueRefreshing == value) return;
            _isMyQueueRefreshing = value;
            OnPropertyChanged();
        }
    }

    private void RefreshMyQueueList()
    {
        CheckForNecessaryUpdate();
        IsMyQueueRefreshing = false;
    }

    private async void Init()
    {
        var getListeningSessionApiResponse = await _listeningSession.GetListeningSessionAsync(_savedJoinId);
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
        if (sessionId == null) return;
        var getCurrentUser = await _userApi.GetCurrentUserAsync(sessionId);
        if (!getListeningSessionApiResponse.IsOk && !getCurrentUser.IsOk)
        {
            DeletionButton.IsVisible = false;
            SearchedPlaylistEntry.IsVisible = false;
            return;
        }

        DeletionButton.IsVisible = SearchedPlaylistEntry.IsVisible =
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
        if (lastUpdated.IsNotFound)
        {
            _timer.Stop();
            MainThread.BeginInvokeOnMainThread(BackToHomeScreen);
        }
        else
        {
            var lastUpdatedDateTime = lastUpdated.Ok()!.QueueLastUpdated;
            if (_lastSavedDateTime.CompareTo(lastUpdatedDateTime) == 0) return;
            if (lastUpdatedDateTime != null)
                _lastSavedDateTime = lastUpdatedDateTime.Value;
            UpdateQueueInformation();
        }
    }

    private async void SearchForSongs(object sender, TextChangedEventArgs e)
    {
        var currentSearchRequest = e.NewTextValue;
        if (string.IsNullOrWhiteSpace(currentSearchRequest) || currentSearchRequest.Length <= 1)
        {
            SearchedTracks.Clear();
            AreSongSearchResultsVisible = false;
            return;
        }

        var allSongsOfSearch =
            await _listeningSession.SearchTrackAsync(_savedJoinId, currentSearchRequest, SearchedTracksLimit);
        if (!allSongsOfSearch.IsOk)
        {
            AreSongSearchResultsVisible = false;
            return;
        }

        var trackMetaData = allSongsOfSearch.Ok()?.Tracks;
        if (trackMetaData == null || trackMetaData.Count == 0)
        {
            AreSongSearchResultsVisible = false;
            return;
        }

        SearchedTracks.Clear();
        foreach (var item in trackMetaData)
            SearchedTracks.Add(item);

        AreSongSearchResultsVisible = !string.IsNullOrWhiteSpace(SearchedSong.Text);
    }

    private async void AddSongToQueue(object sender, ItemTappedEventArgs e)
    {
        var selectedTrack = e.Item as TrackMetaData;
        var username = await SecureStorage.Default.GetAsync(AuthenticationUtil.Username);
        var queue = await _listeningSession.RequestTrackAsync(_savedJoinId,
            new RequestTrackRequest(username, selectedTrack?.SpotifyTrackId));
        if (!queue.IsNoContent) return;

        SearchedSong.Text = "";
        SearchedTracks.Clear();
        UpdateQueueInformation();
    }

    private async void SearchForPlaylist(object sender, TextChangedEventArgs e)
    {
        var currentSearchRequest = e.NewTextValue;
        if (string.IsNullOrWhiteSpace(currentSearchRequest) || currentSearchRequest.Length <= 1)
        {
            SearchedPlaylistsList.Clear();
            ArePlaylistSearchResultsVisible = false;
            return;
        }

        var allPlaylistsOfSearch = await _listeningSession.SearchPlaylistAsync(_savedJoinId, currentSearchRequest);
        if (!allPlaylistsOfSearch.IsOk)
        {
            ArePlaylistSearchResultsVisible = false;
            return;
        }

        var playlistMetaData = allPlaylistsOfSearch.Ok()?.Playlists;
        if (playlistMetaData == null || playlistMetaData.Count == 0)
        {
            ArePlaylistSearchResultsVisible = false;
            return;
        }

        SearchedPlaylistsList.Clear();
        foreach (var item in playlistMetaData)
            SearchedPlaylistsList.Add(item);

        ArePlaylistSearchResultsVisible = !string.IsNullOrWhiteSpace(SearchedPlaylistEntry.Text);
    }

    private async void AddPlaylistToBackground(object sender, ItemTappedEventArgs e)
    {
        var selectedPlaylist = e.Item as PlaylistMetadata;
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
        var queue = await _listeningSession.ChangeFallbackPlaylistAsync(_savedJoinId,
            new ChangeFallbackPlaylistRequest(sessionId, selectedPlaylist?.SpotifyPlaylistId));
        if (!queue.IsNoContent) return;

        SearchedPlaylistEntry.Text = "";
        SearchedPlaylistsList.Clear();
        UpdateQueueInformation();
    }

    private async void DeleteCurrentSession()
    {
        var isAuthenticated = await AuthenticationUtil.IsUserAuthenticated(_authentication);
        if (!isAuthenticated) return;
        var sessionId = await SecureStorage.Default.GetAsync(AuthenticationUtil.SessionId);
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

    private async void OpenSpotifySongLink(object sender, ItemTappedEventArgs e)
    {
        var spotifyTrackUrl = "https://open.spotify.com/track/";

        if (e.Item is not SongRequest selectedTrack) return;
        spotifyTrackUrl += selectedTrack.SpotifyTrackId;
        await Browser.OpenAsync(new Uri(spotifyTrackUrl), BrowserLaunchMode.SystemPreferred);
    }
}