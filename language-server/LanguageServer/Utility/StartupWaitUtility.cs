namespace UnturnedDat.LanguageServer.Utility;

internal class StartupWaitUtility
{
    public bool HasStartedUp { get; private set; }

    private TaskCompletionSource? _tcsStartup = new TaskCompletionSource();

    public Task WaitForStartupAsync()
    {
        return _tcsStartup?.Task ?? Task.CompletedTask;
    }

    internal void NotifyStartupCompleted()
    {
        HasStartedUp = true;
        _tcsStartup?.TrySetResult();
        _tcsStartup = null;
    }
}
