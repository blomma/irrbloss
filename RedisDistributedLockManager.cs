using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Irrbloss;

public class RedisDistributedLockManager(IConnectionMultiplexer connectionMultiplexer)
{
    private sealed class RedisDistributedLock(RedisKey key, RedisValue value)
    {
        public RedisKey Key { get; } = key;

        public RedisValue Value { get; } = value;
    }

    private RedisDistributedLock? _redisDistributedLock;
    private const string UnlockScript = """
                    if redis.call("get",KEYS[1]) == ARGV[1] then
                        return redis.call("del",KEYS[1])
                    else
                        return 0
                    end
        """;

    private static byte[] CreateUniqueLockId()
    {
        return Guid.NewGuid().ToByteArray();
    }

    public async Task<bool> LockAsync(string key, TimeSpan ttl)
    {
        var value = CreateUniqueLockId();

        var db = connectionMultiplexer.GetDatabase();
        var result = await db.StringSetAsync(key, value, ttl, When.NotExists);
        if (!result)
        {
            return false;
        }

        _redisDistributedLock = new(key, value);

        return true;
    }

    public Task UnlockAsync()
    {
        if (_redisDistributedLock == null)
        {
            return Task.CompletedTask;
        }

        RedisKey[] key = [_redisDistributedLock.Key];
        RedisValue[] values = [_redisDistributedLock.Value];

        var db = connectionMultiplexer.GetDatabase();
        return db.ScriptEvaluateAsync(UnlockScript, key, values);
    }
}
