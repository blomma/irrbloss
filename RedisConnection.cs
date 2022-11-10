namespace Irrbloss;

using StackExchange.Redis;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class RedisConnection : IDisposable
{
    private long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
    private DateTimeOffset _firstErrorTime = DateTimeOffset.MinValue;
    private DateTimeOffset _previousErrorTime = DateTimeOffset.MinValue;

    // StackExchange.Redis will also be trying to reconnect internally,
    // so limit how often we recreate the ConnectionMultiplexer instance
    // in an attempt to reconnect
    private readonly TimeSpan ReconnectMinInterval = TimeSpan.FromSeconds(60);

    // If errors occur for longer than this threshold, StackExchange.Redis
    // may be failing to reconnect internally, so we'll recreate the
    // ConnectionMultiplexer instance
    private readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);
    private readonly TimeSpan RestartConnectionTimeout = TimeSpan.FromSeconds(15);

    private readonly SemaphoreSlim _reconnectSemaphore = new(initialCount: 1, maxCount: 1);
    private string? _connectionString;
    private ConnectionMultiplexer? _connection;

    public RedisConnection() { }

    public void Initalize(string connectionString)
    {
        _connectionString = connectionString;

        _connection = ConnectionMultiplexer.Connect(_connectionString);
        _lastReconnectTicks = DateTimeOffset.UtcNow.UtcTicks;
    }

    public async Task<T> BasicRetryAsync<T>(Func<IDatabase, Task<T>> func)
    {
        while (true)
        {
            try
            {
                var connection = _connection;
                if (connection != null)
                {
                    return await func(connection.GetDatabase());
                }
            }
            catch (Exception ex)
                when (ex is RedisConnectionException
                    || ex is SocketException
                    || ex is ObjectDisposedException
                    || ex is RedisTimeoutException
                )
            {
                try
                {
                    await ForceReconnectAsync();
                }
                catch (ObjectDisposedException) { }
            }
        }
    }

    /// <summary>
    /// Force a new ConnectionMultiplexer to be created.
    /// NOTES:
    ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnectAsync().
    ///     2. Call ForceReconnectAsync() for RedisConnectionExceptions and RedisSocketExceptions. You can also call it for RedisTimeoutExceptions,
    ///         but only if you're using generous ReconnectMinInterval and ReconnectErrorThreshold. Otherwise, establishing new connections can cause
    ///         a cascade failure on a server that's timing out because it's already overloaded.
    ///     3. The code will:
    ///         a. wait to reconnect for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
    ///         b. not reconnect more frequently than configured in "ReconnectMinInterval"
    /// </summary>
    private async Task ForceReconnectAsync()
    {
        long previousTicks = Interlocked.Read(ref _lastReconnectTicks);
        var previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
        TimeSpan elapsedSinceLastReconnect = DateTimeOffset.UtcNow - previousReconnectTime;

        // We want to limit how often we perform this top-level reconnect, so we check how long it's been since our last attempt.
        if (elapsedSinceLastReconnect < ReconnectMinInterval)
        {
            return;
        }

        try
        {
            if (!await _reconnectSemaphore.WaitAsync(RestartConnectionTimeout))
                return;
        }
        catch
        {
            return;
        }

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            elapsedSinceLastReconnect = utcNow - previousReconnectTime;

            if (_firstErrorTime == DateTimeOffset.MinValue)
            {
                // We haven't seen an error since last reconnect, so set initial values.
                _firstErrorTime = utcNow;
                _previousErrorTime = utcNow;
                return;
            }

            if (elapsedSinceLastReconnect < ReconnectMinInterval)
            {
                // Some other thread made it through the check and the lock, so nothing to do.
                return;
            }

            TimeSpan elapsedSinceFirstError = utcNow - _firstErrorTime;
            TimeSpan elapsedSinceMostRecentError = utcNow - _previousErrorTime;

            bool shouldReconnect =
                elapsedSinceFirstError >= ReconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                && elapsedSinceMostRecentError <= ReconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
            _previousErrorTime = utcNow;

            if (!shouldReconnect)
            {
                return;
            }

            _firstErrorTime = DateTimeOffset.MinValue;
            _previousErrorTime = DateTimeOffset.MinValue;

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                }
                catch
                {
                    // Ignore any errors from the old connection
                }
            }

            Interlocked.Exchange(ref _connection, null);
            ConnectionMultiplexer newConnection = await ConnectionMultiplexer.ConnectAsync(
                _connectionString!
            );
            Interlocked.Exchange(ref _connection, newConnection);
            Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
