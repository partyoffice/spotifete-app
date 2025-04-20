using spotifete.Sessions;

namespace spotifete;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }


    private void JoinSession_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new JoinSession());
    }

    private void CreateSession_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CreateSession());
    }
}