using Moq;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using Betsson.OnlineWallets.Data;

public class OnlineWalletRepositoryTests
{
    private readonly Mock<OnlineWalletContext> _dbContextMock;
    private readonly Mock<DbSet<OnlineWalletEntry>> _dbSetMock;
    private readonly OnlineWalletRepository _repository;

    public OnlineWalletRepositoryTests()
    {
        _dbContextMock = new Mock<OnlineWalletContext>();
        _dbSetMock = new Mock<DbSet<OnlineWalletEntry>>();

        _dbContextMock.Setup(context => context.Transactions).Returns(_dbSetMock.Object);
        _repository = new OnlineWalletRepository(_dbContextMock.Object);
    }

    [Fact]
    public async Task GetLastOnlineWalletEntryAsync_ShouldReturnLastTransaction()
    {
        // Arrange
        var transactions = new List<OnlineWalletEntry>
        {
            new OnlineWalletEntry { EventTime = DateTimeOffset.UtcNow.AddMinutes(-10), Amount = 50m },
            new OnlineWalletEntry { EventTime = DateTimeOffset.UtcNow.AddMinutes(-5), Amount = 100m }
        }.AsQueryable();

        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.Provider).Returns(transactions.Provider);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.Expression).Returns(transactions.Expression);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.ElementType).Returns(transactions.ElementType);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.GetEnumerator()).Returns(transactions.GetEnumerator());

        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(100m);
        result.EventTime.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetLastOnlineWalletEntryAsync_ShouldReturnNull_WhenNoTransactionsExist()
    {
        // Arrange
        var emptyTransactions = new List<OnlineWalletEntry>().AsQueryable();

        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.Provider).Returns(emptyTransactions.Provider);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.Expression).Returns(emptyTransactions.Expression);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.ElementType).Returns(emptyTransactions.ElementType);
        _dbSetMock.As<IQueryable<OnlineWalletEntry>>().Setup(m => m.GetEnumerator()).Returns(emptyTransactions.GetEnumerator());

        // Act
        var result = await _repository.GetLastOnlineWalletEntryAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertOnlineWalletEntryAsync_ShouldAddEntryToTransactions()
    {
        // Arrange
        var newEntry = new OnlineWalletEntry { Amount = 200m, EventTime = DateTimeOffset.UtcNow };

        _dbSetMock.Setup(m => m.Add(It.IsAny<OnlineWalletEntry>())).Verifiable();
        _dbContextMock.Setup(m => m.SaveChanges()).Verifiable();

        // Act
        await _repository.InsertOnlineWalletEntryAsync(newEntry);

        // Assert
        _dbSetMock.Verify(m => m.Add(It.Is<OnlineWalletEntry>(entry => 
            entry.Amount == newEntry.Amount && 
            entry.EventTime == newEntry.EventTime)), 
        Times.Once);

        _dbContextMock.Verify(m => m.SaveChanges(), Times.Once);
    }
}