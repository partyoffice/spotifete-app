using System.Collections.ObjectModel;
using Org.OpenAPITools.Api;
using Org.OpenAPITools.Model;
using Timer = System.Timers.Timer;

namespace spotifete.Sessions;

public partial class CurrentSession : ContentPage
{
    private const double RefreshInterval = 5000;
    private const int SearchedTracksLimit = 25;
    private readonly IListeningSessionApi _listeningSession;
    private readonly string _savedSessionId;
    private DateTime _lastSavedDateTime = DateTime.Now;
    private Timer _timer = new();

    public CurrentSession(FullListeningSession fullListeningSession,
        IListeningSessionApi listeningSession)
    {
        InitializeComponent();
        BindingContext = this;

        _listeningSession = listeningSession;
        _savedSessionId = fullListeningSession.JoinId == null ? " " : fullListeningSession.JoinId;
        SessionTitle.Text = fullListeningSession.Title == null ? " " : fullListeningSession.Title;
        SessionCode.Text = "Session code: " + _savedSessionId;

        UpdateQueueInformation();
        SetTimer();
    }

    public ObservableCollection<SongRequest> CurrentQueue { get; private set; } = new();

    public ObservableCollection<TrackMetaData> SearchedTracks { get; } = new();

    private async void UpdateQueueInformation()
    {
        var queueLastUpdatedAsync = await _listeningSession.GetSessionQueueAsync(_savedSessionId);
        if (!queueLastUpdatedAsync.IsOk) return;
        CurrentQueue.Clear();
        foreach (var item in queueLastUpdatedAsync.Ok().Queue) CurrentQueue.Add(item);
    }

    private async void CheckForNeccessaryUpdate()
    {
        var lastUpdated = await _listeningSession.QueueLastUpdatedAsync(_savedSessionId);

        var lastUpdatedDateTime = DateTime.Now;
        if (lastUpdated.IsOk) lastUpdatedDateTime = lastUpdated.Ok().QueueLastUpdated.Value;

        if (lastUpdatedDateTime.CompareTo(_lastSavedDateTime) == 0) return;
        _lastSavedDateTime = lastUpdatedDateTime;
        UpdateQueueInformation();
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
            await _listeningSession.SearchTrackAsync(_savedSessionId, currentSearchRequest, SearchedTracksLimit);
        if (!allSongsOfSearch.IsOk) return;
        SearchedTracks.Clear();
        foreach (var item in allSongsOfSearch.Ok().Tracks) SearchedTracks.Add(item);
    }

    private async void AddSongToQueue(object sender, SelectedItemChangedEventArgs e)
    {
        var selectedTrack = e.SelectedItem as TrackMetaData;
        var queue = await _listeningSession.RequestTrackAsync(_savedSessionId,
            new RequestTrackRequest(" ", selectedTrack.SpotifyTrackId));
        if (!queue.IsNoContent) return;

        SearchedSong.Text = "";
        SearchedTracks.Clear();
        UpdateQueueInformation();
    }

    private void SetTimer()
    {
        _timer = new Timer(RefreshInterval);
        _timer.Elapsed += (sender, e) => CheckForNeccessaryUpdate();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    protected override bool OnBackButtonPressed()
    {
        _timer.Stop();
        return base.OnBackButtonPressed();
    }
}