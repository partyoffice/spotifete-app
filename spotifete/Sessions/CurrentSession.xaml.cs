using System.Collections.ObjectModel;
using Org.OpenAPITools.Api;
using Org.OpenAPITools.Model;
using Timer = System.Timers.Timer;

namespace spotifete.Sessions;

public partial class CurrentSession : ContentPage
{
    private const double RefreshInterval = 5000;
    private readonly IListeningSessionApi _listeningSession;
    private readonly string _savedSessionId;
    private DateTime _lastSavedDateTime = DateTime.Now;
    private Timer timer;

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

    private void SetTimer()
    {
        timer = new Timer(RefreshInterval);
        timer.Elapsed += (sender, e) => CheckForNeccessaryUpdate();
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    protected override bool OnBackButtonPressed()
    {
        timer.Stop();
        return base.OnBackButtonPressed();
    }
}