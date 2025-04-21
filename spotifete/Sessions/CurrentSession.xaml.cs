using Org.OpenAPITools.Api;

namespace spotifete.Sessions;

public partial class CurrentSession : ContentPage
{
    public CurrentSession(IGetListeningSessionApiResponse listeningSessionApiResponse)
    {
        InitializeComponent();
    }
}