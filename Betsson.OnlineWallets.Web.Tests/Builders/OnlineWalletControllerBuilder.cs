using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Web.Controllers;
using Betsson.OnlineWallets.Web.Models;

namespace Betsson.OnlineWallets.Web.Tests.Builders
{
    public class OnlineWalletControllerBuilder
    {
        private Mock<ILogger<OnlineWalletController>> _mockLogger = new Mock<ILogger<OnlineWalletController>>();
        private Mock<IMapper> _mockMapper = new Mock<IMapper>();
        private Mock<IOnlineWalletService> _mockService = new Mock<IOnlineWalletService>();

        private Balance _balance = new Balance { Amount = 100.00m }; // Default balance
        private Deposit _deposit = new Deposit { Amount = 50.00m };  // Default deposit
        private Withdrawal _withdrawal = new Withdrawal { Amount = 30.00m }; // Default withdrawal

        public OnlineWalletControllerBuilder WithBalance(decimal amount)
        {
            _balance = new Balance { Amount = amount };
            _mockService.Setup(service => service.GetBalanceAsync())
                .ReturnsAsync(_balance);

            _mockMapper.Setup(mapper => mapper.Map<BalanceResponse>(_balance))
                .Returns(new BalanceResponse { Amount = _balance.Amount });

            return this;
        }

        public OnlineWalletControllerBuilder WithDeposit(DepositRequest depositRequest)
        {
            _deposit = new Deposit { Amount = depositRequest.Amount };
            _mockMapper.Setup(mapper => mapper.Map<Deposit>(depositRequest))
                .Returns(_deposit);

            _mockService.Setup(service => service.DepositFundsAsync(_deposit))
                .ReturnsAsync(new Balance { Amount = _balance.Amount + _deposit.Amount });

            _mockMapper.Setup(mapper => mapper.Map<BalanceResponse>(It.IsAny<Balance>()))
                .Returns(new BalanceResponse { Amount = _balance.Amount + _deposit.Amount });

            return this;
        }

        public OnlineWalletControllerBuilder WithWithdrawal(WithdrawalRequest withdrawalRequest)
        {
            _withdrawal = new Withdrawal { Amount = withdrawalRequest.Amount };
            _mockMapper.Setup(mapper => mapper.Map<Withdrawal>(withdrawalRequest))
                .Returns(_withdrawal);

            _mockService.Setup(service => service.WithdrawFundsAsync(_withdrawal))
                .ReturnsAsync(new Balance { Amount = _balance.Amount - _withdrawal.Amount });

            _mockMapper.Setup(mapper => mapper.Map<BalanceResponse>(It.IsAny<Balance>()))
                .Returns(new BalanceResponse { Amount = _balance.Amount - _withdrawal.Amount });

            return this;
        }

        public OnlineWalletController Build()
        {
            return new OnlineWalletController(_mockLogger.Object, _mockMapper.Object, _mockService.Object);
        }

        public Mock<IOnlineWalletService> MockService => _mockService;
        public Mock<IMapper> MockMapper => _mockMapper;
    }
}