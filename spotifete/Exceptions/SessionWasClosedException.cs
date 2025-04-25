namespace spotifete.Exceptions;

public class SessionWasClosedException
{
    public static void DisplayAlert(ContentPage contentPage)
    {
        contentPage.DisplayAlert("Session closed", "The current Session was closed by the Owner", "OK");
    }
}