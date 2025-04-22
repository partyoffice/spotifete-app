using Android.App;
using Android.Content;
using Android.Content.PM;

namespace spotifete.Droid;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = CALLBACK_SCHEME,
    DataHost = CALLBACK_HOST,
    DataPath = CALLBACK_PATH)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
    private const string CALLBACK_SCHEME = "https";
    private const string CALLBACK_HOST = "spotifete.nikos410.de";
    private const string CALLBACK_PATH = "/app/android";
}