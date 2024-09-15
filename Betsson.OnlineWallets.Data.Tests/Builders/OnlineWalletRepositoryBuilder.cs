using Betsson.OnlineWallets.Data.Models;

namespace Betsson.OnlineWallets.Data.Tests.Builders;

public class OnlineWalletRepositoryBuilder
{
    private readonly OnlineWalletEntry _entry;

    public OnlineWalletRepositoryBuilder()
    {
        _entry = new OnlineWalletEntry
        {
            EventTime = DateTimeOffset.UtcNow, // Default value
            Amount = 0m // Default value
        };
    }

    public OnlineWalletRepositoryBuilder WithAmount(decimal amount)
    {
        _entry.Amount = amount;
        return this;
    }

    public OnlineWalletRepositoryBuilder WithEventTime(DateTimeOffset eventTime)
    {
        _entry.EventTime = eventTime;
        return this;
    }

    public OnlineWalletEntry Build() => _entry;
}
