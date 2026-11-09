namespace vatSysToolbox;

/// <summary>
/// Runs a long-running worker that you can start and stop.
/// The worker must stop when its cancellation token is cancelled.
/// </summary>
public class BackgroundTask(Func<CancellationToken, Task> worker) : IAsyncDisposable
{
    bool IsDisposed { get; set; }

    CancellationTokenSource? _stoppingTokenSource;
    Task? _executingTask;

    public bool IsRunning => _executingTask is { IsCompleted: false };

    public void Start()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(BackgroundTask));

        if (IsRunning)
            return;

        _stoppingTokenSource?.Dispose();
        _stoppingTokenSource = new CancellationTokenSource();
        _executingTask = worker(_stoppingTokenSource.Token);
    }

    public async Task Stop()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(BackgroundTask));

        await StopInternal();
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;

        await StopInternal();

        _executingTask?.Dispose();
        _stoppingTokenSource?.Dispose();
        IsDisposed = true;
    }

    async Task StopInternal()
    {
        if (_executingTask is null || _stoppingTokenSource is null)
            return;

        _stoppingTokenSource.Cancel();

        try
        {
            await _executingTask;
        }
        catch (OperationCanceledException) when (_stoppingTokenSource.IsCancellationRequested)
        {
            // Expected when the worker stops because of the cancellation
        }
    }
}
