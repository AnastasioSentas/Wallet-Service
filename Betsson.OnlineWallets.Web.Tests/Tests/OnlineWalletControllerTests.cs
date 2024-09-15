using Betsson.OnlineWallets.Web.Models;
using Betsson.OnlineWallets.Web.Tests.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Betsson.OnlineWallets.Web.Tests.Tests
{
    public class OnlineWalletControllerTests
    {
        [Fact]
        public async Task Balance_ShouldReturnCorrectAmount()
        {
            // Arrange
            var controller = new OnlineWalletControllerBuilder()
                .WithBalance(200.00m)
                .Build();

            // Act
            var result = await controller.Balance();

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            var balanceResponse = actionResult.Value.Should().BeOfType<BalanceResponse>().Subject;
            balanceResponse.Amount.Should().Be(200.00m);
        }

        [Fact]
        public async Task Deposit_ShouldReturnUpdatedBalance()
        {
            // Arrange
            var controller = new OnlineWalletControllerBuilder()
                .WithBalance(150.00m)
                .WithDeposit(new DepositRequest { Amount = 50.00m })
                .Build();

            // Act
            var result = await controller.Deposit(new DepositRequest { Amount = 50.00m });

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var balanceResponse = actionResult.Value.Should().BeOfType<BalanceResponse>().Subject;
            balanceResponse.Amount.Should().Be(200.00m);
        }

        [Fact]
        public async Task Withdraw_ShouldReturnUpdatedBalance()
        {
            // Arrange
            var controller = new OnlineWalletControllerBuilder()
                .WithBalance(100.00m)
                .WithWithdrawal(new WithdrawalRequest { Amount = 30.00m })
                .Build();

            // Act
            var result = await controller.Withdraw(new WithdrawalRequest { Amount = 30.00m });

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var balanceResponse = actionResult.Value.Should().BeOfType<BalanceResponse>().Subject;
            balanceResponse.Amount.Should().Be(70.00m);
        }
    }
}