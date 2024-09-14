using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Betsson.OnlineWallets.Data;

public class OnlineWalletRepositoryTests : IDisposable
{
    private readonly OnlineWalletRepository repo; OnlineWalletContext _context;
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
        _context.Dispose();
    }

    [Fact]
    public async Task GetLastOnlineWalletEntryAsync_ReturnsMostRecentTransactionBasedOnEventTime()
    {
        //Arrange
        var entry1 = new OnlineWalletEntry() { EventTime = DateTimeOffset.Now.AddMinutes(-10), Amount = 50m };
        var entry2 = new OnlineWalletEntry() { EventTime = DateTimeOffset.Now.AddMinutes(-5), Amount = 100m };
        _context.Transactions.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(100m);
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
    public async Task InsertOnlineWalletEntryAsync_ShouldAddEntryToDatabase()
    {
        // Arrange
        var newEntry = new OnlineWalletEntry { Amount = 200m, EventTime = DateTimeOffset.UtcNow };
        

        // Act
        await _repository.InsertOnlineWalletEntryAsync(newEntry);

        // Assert
        var result = await _context.Transactions.FirstOrDefaultAsync(e => e.Amount == 200m);

        result.Should().NotBeNull();
        result.Amount.Should().Be(200m);
    }
}