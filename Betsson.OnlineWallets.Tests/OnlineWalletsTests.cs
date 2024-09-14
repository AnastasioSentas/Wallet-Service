using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using FluentAssertions;
using Moq;

namespace Betsson.OnlineWallets.Tests;

public class OnlineWalletServiceTests
{
    private readonly Mock<IOnlineWalletRepository> _onlineWalletRepositoryMock;
    private readonly OnlineWalletService _onlineWalletService;

    public OnlineWalletServiceTests()
    {
        _onlineWalletRepositoryMock = new Mock<IOnlineWalletRepository>();
        _onlineWalletService = new OnlineWalletService(_onlineWalletRepositoryMock.Object);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnZero_WhenNoTransactionsExist()
    {
        // Arrange
        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync((OnlineWalletEntry)null);

        // Act
        var result = await _onlineWalletService.GetBalanceAsync();

        // Assert
        result.Amount.Should().Be(0);
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnCorrectBalance_WhenTransactionsExist()
    {
        // Arrange
        var walletEntry = new OnlineWalletEntry
        {
            BalanceBefore = 100m,
            Amount = 50m
        };

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(walletEntry);

        // Act
        var result = await _onlineWalletService.GetBalanceAsync();

        // Assert
        result.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task DepositFundsAsync_ShouldAddDepositAmountToCurrentBalance()
    {
        // Arrange
        var deposit = new Deposit { Amount = 100m };

        var currentBalanceEntry = new OnlineWalletEntry
        {
            BalanceBefore = 200m,
            Amount = 0m
        };

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(currentBalanceEntry);

        // Act
        var result = await _onlineWalletService.DepositFundsAsync(deposit);

        // Assert
        result.Amount.Should().Be(300m);
        _onlineWalletRepositoryMock.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
    }
    
    [Fact]
    public async Task DepositFundsAsync_ShouldNotAddDepositAmountToCurrentBalance_WhenDepositAmountIsZero()
    {
        // Arrange
        var deposit = new Deposit { Amount = 0m };

        var currentBalanceEntry = new OnlineWalletEntry
        {
            BalanceBefore = 100m,
            Amount = 0m
        };

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(currentBalanceEntry);

        // Act
        var result = await _onlineWalletService.DepositFundsAsync(deposit);

        // Assert
        result.Amount.Should().Be(100m);
        _onlineWalletRepositoryMock.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
    }
    
    [Fact]
    public async Task DepositFundsAsync_ShouldSetEventTime_WhenDepositIsMade()
    {
        // Arrange
        var deposit = new Deposit { Amount = 100m };
        var currentBalanceEntry = new OnlineWalletEntry
        {
            BalanceBefore = 200m,
            Amount = 0m
        };

        OnlineWalletEntry entry = null;

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(currentBalanceEntry);

        _onlineWalletRepositoryMock.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
            .Callback<OnlineWalletEntry>(newEntry => entry = newEntry); // using callback here to capture and assert the onlineWalletEntry

        // Act
        await _onlineWalletService.DepositFundsAsync(deposit);

        // Assert
        entry.Should().NotBeNull();
        entry.Amount.Should().Be(deposit.Amount);
        entry.BalanceBefore.Should().Be(currentBalanceEntry.BalanceBefore);
        entry.EventTime.Should().NotBe(default(DateTimeOffset)); // verify that the EventTime property has been set to a value other than its default value.
    }

    [Fact]
    public async Task WithdrawFundsAsync_ShouldThrowException_WhenWithdrawalExceedsBalance()
    {
        // Arrange
        var withdrawal = new Withdrawal { Amount = 300m };

        var currentBalanceEntry = new OnlineWalletEntry
        {
            BalanceBefore = 100m,
            Amount = 0m
        };

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(currentBalanceEntry);

        // Act & Assert
        await _onlineWalletService
            .Invoking(service => service.WithdrawFundsAsync(withdrawal))
            .Should()
            .ThrowAsync<InsufficientBalanceException>();
    }

    [Fact]
    public async Task WithdrawFundsAsync_ShouldDeductAmount_WhenWithdrawalIsValid()
    {
        // Arrange
        var withdrawal = new Withdrawal { Amount = 50m };

        var currentBalanceEntry = new OnlineWalletEntry
        {
            BalanceBefore = 100m,
            Amount = 0m
        };

        _onlineWalletRepositoryMock.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
            .ReturnsAsync(currentBalanceEntry);

        // Act
        var result = await _onlineWalletService.WithdrawFundsAsync(withdrawal);

        // Assert
        result.Amount.Should().Be(50m); // 100 (current balance) - 50 (withdrawal)
        _onlineWalletRepositoryMock.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
    }
}