namespace spotifete.Exceptions;

public abstract class SessionDoesNotExistException
{
    public static void DisplayAlert(ContentPage contentPage, string sessionCode)
    {
        contentPage.DisplayAlert("Session does not exist", "The requested Session " + sessionCode + " does not exist.",
            "OK");
    }
}