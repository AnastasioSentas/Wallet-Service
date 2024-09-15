using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Tests.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Betsson.OnlineWallets.Data.Tests;

public class OnlineWalletRepositoryTests : IDisposable
{
    private readonly OnlineWalletContext _context;
    private readonly OnlineWalletRepository _repository;

    public OnlineWalletRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OnlineWalletContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        _context = new OnlineWalletContext(options);
        _repository = new OnlineWalletRepository(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task GetLastOnlineWalletEntryAsync_ShouldReturnMostRecentTransaction_WhenMultipleTransactionsExist()
    {
        // Arrange
        var entry1 = new OnlineWalletRepositoryBuilder()
            .WithAmount(50m)
            .WithEventTime(DateTimeOffset.Now.AddMinutes(-10))
            .Build();

        var entry2 = new OnlineWalletRepositoryBuilder()
            .WithAmount(100m)
            .WithEventTime(DateTimeOffset.Now.AddMinutes(-5))
            .Build();

        _context.Transactions.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(100m);
        result.EventTime.Should().Be(entry2.EventTime);
    }

    [Fact]
    public async Task GetLastOnlineWalletEntryAsync_ShouldReturnNull_WhenNoTransactionsExist()
    {
        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertOnlineWalletEntryAsync_ShouldAddEntryToDatabaseSuccessfully()
    {
        // Arrange
        var newEntry = new OnlineWalletRepositoryBuilder()
            .WithAmount(200m)
            .WithEventTime(DateTimeOffset.UtcNow)
            .Build();

        // Act
        await _repository.InsertOnlineWalletEntryAsync(newEntry);

        // Assert
        var result = await _context.Transactions.FirstOrDefaultAsync(e => e.Amount == 200m);
        result.Should().NotBeNull();
        result.Amount.Should().Be(200m);
        result.EventTime.Should().Be(newEntry.EventTime);
    }
}