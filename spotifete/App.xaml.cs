namespace spotifete;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage())
        {
            BarTextColor = Colors.Black
        };
    }
}