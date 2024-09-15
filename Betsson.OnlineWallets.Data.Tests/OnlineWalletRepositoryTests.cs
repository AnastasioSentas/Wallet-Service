using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
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
    public async Task GetLastOnlineWalletEntryAsync_ReturnsMostRecentTransaction_WhenMultipleTransactionsExist()
    {
        // Arrange
        var entry1 = new OnlineWalletEntry { EventTime = DateTimeOffset.Now.AddMinutes(-10), Amount = 50m };
        var entry2 = new OnlineWalletEntry { EventTime = DateTimeOffset.Now.AddMinutes(-5), Amount = 100m };
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
    public async Task GetLastOnlineWalletEntryAsync_ReturnsNull_WhenNoTransactionsExist()
    {
        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertOnlineWalletEntryAsync_AddsEntryToDatabaseSuccessfully()
    {
        // Arrange
        var newEntry = new OnlineWalletEntry { Amount = 200m, EventTime = DateTimeOffset.UtcNow };

        // Act
        await _repository.InsertOnlineWalletEntryAsync(newEntry);

        // Assert
        var result = await _context.Transactions.FirstOrDefaultAsync(e => e.Amount == 200m);
        result.Should().NotBeNull();
        result.Amount.Should().Be(200m);
        result.EventTime.Should().Be(newEntry.EventTime);
    }
}