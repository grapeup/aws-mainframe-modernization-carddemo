using System.Threading;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

/// <summary>
/// Provides per-test unique account IDs and card numbers so tests never collide.
/// Account IDs start at 9_000_000 and increment. Card numbers are 16-char zero-padded.
/// </summary>
public static class UniqueIds
{
    private static long _counter = 9_000_000;

    public static long NextAccountId() => Interlocked.Increment(ref _counter);

    public static string CardNumberFor(long accountId) =>
        accountId.ToString().PadLeft(16, '0');
}
