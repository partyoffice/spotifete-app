using Org.OpenAPITools.Api;
using spotifete.Sessions;
using spotifete.SpotifeteApi;

namespace spotifete;

public partial class MainPage : ContentPage
{
    private readonly IListeningSessionApi _listeningSession;

    public MainPage()
    {
        InitializeComponent();
        _listeningSession = ApiHelper.Instance().ListeningSessionApi;
    }

    private async void OnEnteredSessionID(object sender, EventArgs e)
    {
        var getListeningSessionApiResponse = await _listeningSession.GetListeningSessionAsync(SessionId.Text);
        if (getListeningSessionApiResponse.IsOk)
            await Navigation.PushAsync(new CurrentSession(getListeningSessionApiResponse));
    }

    private void CreateSession_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CreateSession());
    }
}