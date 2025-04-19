# Spotifete-API

This readme will teach you how to use the client and not the API in general. The client was build with the
OpenAPI-Generator CLI. To make it even easier I build an ApiHelper singleton. Here is a simple example:

Init the ApiHelper in your class:

```csharp
public class CoolClass
{
    private IListeningSessionApi _listeningSession;
    
    public CoolClass() 
    {
        _listeningSession = ApiHelper.Instance().ListeningSessionApi;
    }
}
```

Here is how to use it:

```csharp
private async void OnCounterClicked() 
    {
        var queueList = await _listeningSession.GetSessionQueueAsync("123");
        ...
    }
```