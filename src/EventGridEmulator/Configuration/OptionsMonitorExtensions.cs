using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.Configuration;

/// <summary>
/// When the appsettings.json file changes, the underlying ChangeToken is fired twice.
/// This class ensure we only execute the callback once.
/// https://github.com/dotnet/aspnetcore/issues/2542
/// </summary>
internal static class OptionsMonitorExtensions
{
    private const int DefaultDelay = 1000;

    private static readonly ConcurrentDictionary<object, CancellationTokenSource> Tokens = new ConcurrentDictionary<object, CancellationTokenSource>();

    public static IDisposable? OnChangeDelayed<T>(this IOptionsMonitor<T> monitor, Action<T> listener, int delay = DefaultDelay)
    {
        return monitor.OnChangeDelayed((options, _) => listener(options), delay);
    }

    public static IDisposable? OnChangeDelayed<T>(this IOptionsMonitor<T> monitor, Action<T, string?> listener, int delay = DefaultDelay)
    {
        return monitor.OnChange((options, name) => ChangeHandler(monitor, listener, options, name, delay));
    }

    private static void ChangeHandler<T>(IOptionsMonitor<T> monitor, Action<T, string?> listener, T options, string? name, int delay)
    {
        var tokenSource = GetCancellationTokenSource(monitor);
        var token = tokenSource.Token;
        var delayTask = Task.Delay(delay, token);

        delayTask.ContinueWith(_ => ListenerInvoker(monitor, listener, options, name), token);
    }

    private static CancellationTokenSource GetCancellationTokenSource<T>(IOptionsMonitor<T> monitor)
    {
        return Tokens.AddOrUpdate(monitor, CreateTokenSource, ReplaceTokenSource);
    }

    private static CancellationTokenSource CreateTokenSource(object key)
    {
        return new CancellationTokenSource();
    }

    private static CancellationTokenSource ReplaceTokenSource(object key, CancellationTokenSource existingCts)
    {
        existingCts.Cancel();
        existingCts.Dispose();
        return new CancellationTokenSource();
    }

    private static void ListenerInvoker<T>(IOptionsMonitor<T> monitor, Action<T, string?> listener, T options, string? name)
    {
        listener(options, name);

        if (Tokens.TryRemove(monitor, out var cts))
        {
            cts.Dispose();
        }
    }
}