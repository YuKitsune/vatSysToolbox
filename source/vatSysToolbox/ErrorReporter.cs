using Microsoft.Extensions.Logging;

namespace vatSysToolbox;

public interface IErrorReporter
{
    void ReportError(Exception exception);
    void ReportError(Exception exception, string message);
}

/// <summary>
/// Writes errors to the log, and shows them in the vatSys error window.
/// The error window shows each error message one time each minute at most, so that the same error does not flood
/// the error window. You can use this class from all threads.
/// </summary>
public class ErrorReporter(string source, ILogger<ErrorReporter> logger, IClock clock) : IErrorReporter
{
    static readonly TimeSpan RepeatInterval = TimeSpan.FromMinutes(1);

    readonly Dictionary<string, DateTimeOffset> _lastShownTimes = new();

    public void ReportError(Exception exception)
    {
        logger.LogError(exception, "An error occurred");
        ShowError(exception);
    }

    public void ReportError(Exception exception, string message)
    {
        logger.LogError(exception, message);
        ShowError(exception);
    }

    void ShowError(Exception exception)
    {
        lock (_lastShownTimes)
        {
            var now = clock.UtcNow();

            if (_lastShownTimes.TryGetValue(exception.Message, out var lastShown) &&
                now - lastShown <= RepeatInterval)
                return;

            vatsys.Errors.Add(exception, source);
            _lastShownTimes[exception.Message] = now;
        }
    }
}
