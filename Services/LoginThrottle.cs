using System.Collections.Concurrent;

namespace AIClassroom.Services;

/// <summary>
/// In-memory brute-force protection for login. Two layers:
///  - per (IP + account): 5 failures -> 5 minute lock
///  - per account (any IP): 30 failures -> 15 minute lock (stops distributed PIN guessing)
/// Single-server only; move to a distributed cache if you scale out.
/// </summary>
public class LoginThrottle
{
    private record Entry(int Fails, DateTime WindowStartUtc, DateTime? LockedUntilUtc);
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    private static readonly (int Max, TimeSpan Lock, TimeSpan Window) PerIp = (5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
    private static readonly (int Max, TimeSpan Lock, TimeSpan Window) PerAccount = (30, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30));

    private static string IpKey(string ip, string account) => $"ip:{ip}|{account.ToLowerInvariant()}";
    private static string AccKey(string account) => $"acc:{account.ToLowerInvariant()}";

    public bool IsLocked(string ip, string account)
        => IsLockedKey(IpKey(ip, account)) || IsLockedKey(AccKey(account));

    private bool IsLockedKey(string key)
        => _entries.TryGetValue(key, out var e) && e.LockedUntilUtc.HasValue && e.LockedUntilUtc > DateTime.UtcNow;

    public void RecordFailure(string ip, string account)
    {
        Bump(IpKey(ip, account), PerIp);
        Bump(AccKey(account), PerAccount);
        if (_entries.Count > 20_000) Prune();
    }

    public void RecordSuccess(string ip, string account) => _entries.TryRemove(IpKey(ip, account), out _);

    private void Bump(string key, (int Max, TimeSpan Lock, TimeSpan Window) p)
    {
        var now = DateTime.UtcNow;
        _entries.AddOrUpdate(key,
            _ => new Entry(1, now, null),
            (_, e) =>
            {
                if (now - e.WindowStartUtc > p.Window && !(e.LockedUntilUtc > now)) return new Entry(1, now, null);
                var fails = e.Fails + 1;
                return fails >= p.Max ? new Entry(0, now, now + p.Lock) : e with { Fails = fails };
            });
    }

    private void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _entries)
            if ((kv.Value.LockedUntilUtc ?? kv.Value.WindowStartUtc.AddHours(1)) < now) _entries.TryRemove(kv.Key, out _);
    }
}
