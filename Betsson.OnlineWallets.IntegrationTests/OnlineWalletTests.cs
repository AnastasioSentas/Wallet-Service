using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnlineWallets.IntegrationTests.Models;

using Betsson.OnlineWallets.Data;
using Betsson.OnlineWallets.IntegrationTests.Factories;

namespace Betsson.OnlineWallets.IntegrationTests
{
    public class OnlineWalletTests : IClassFixture<CustomWebApplicationFactory<Betsson.OnlineWallets.Web.Startup>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly OnlineWalletContext _context;
        private readonly CustomWebApplicationFactory<Betsson.OnlineWallets.Web.Startup> _factory;


        public OnlineWalletTests(CustomWebApplicationFactory<Betsson.OnlineWallets.Web.Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            
            var options = new DbContextOptionsBuilder<OnlineWalletContext>()
                .UseInMemoryDatabase("testDB") 
                .Options;
            _context = new OnlineWalletContext(options);

            // Ensure the database is reset before each test
            ResetDatabase();
        }
        
        private void ResetDatabase()
        {
            _context.Database.EnsureDeleted(); // Deletes the current in-memory database
            _context.Database.EnsureCreated(); // Recreates the in-memory database
        }

        [Fact]
        public async Task Balance_Endpoint_ShouldReturnCurrentBalance()
        {
            // Arrange
            var depositPayload = new
            {
                Amount = 200
            };

            var depositContent = new StringContent(JsonConvert.SerializeObject(depositPayload), Encoding.UTF8, "application/json");
            await _client.PostAsync("/OnlineWallet/Deposit", depositContent);

            // Act
            var response = await _client.GetAsync("/OnlineWallet/Balance");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var balanceResponse = JsonConvert.DeserializeObject<BalanceResponse>(responseString);

            balanceResponse.Should().NotBeNull();
            balanceResponse.Amount.Should().Be(200);
        }

        [Fact]
        public async Task Deposit_Endpoint_ShouldIncreaseBalance()
        {
            // Arrange
            var initialDeposit = new
            {
                Amount = 100
            };

            var initialDepositContent = new StringContent(JsonConvert.SerializeObject(initialDeposit), Encoding.UTF8, "application/json");
            await _client.PostAsync("/OnlineWallet/Deposit", initialDepositContent);

            var depositPayload = new
            {
                Amount = 50
            };

            var depositContent = new StringContent(JsonConvert.SerializeObject(depositPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Deposit", depositContent);

            // Assert
            response.EnsureSuccessStatusCode(); 
            var responseString = await response.Content.ReadAsStringAsync();
            var balanceResponse = JsonConvert.DeserializeObject<BalanceResponse>(responseString);

            balanceResponse.Should().NotBeNull();
            balanceResponse.Amount.Should().Be(150);
        }

        [Fact]
        public async Task Withdraw_Endpoint_ShouldDecreaseBalance()
        {
            // Arrange
            var depositPayload = new
            {
                Amount = 100
            };

            var depositContent = new StringContent(JsonConvert.SerializeObject(depositPayload), Encoding.UTF8, "application/json");
            await _client.PostAsync("/OnlineWallet/Deposit", depositContent);

            var withdrawPayload = new
            {
                Amount = 50
            };

            var withdrawContent = new StringContent(JsonConvert.SerializeObject(withdrawPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Withdraw", withdrawContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var balanceResponse = JsonConvert.DeserializeObject<BalanceResponse>(responseString);

            balanceResponse.Should().NotBeNull();
            balanceResponse.Amount.Should().Be(50);
        }

        [Fact]
        public async Task Withdraw_Endpoint_ShouldReturnBadRequest_WhenInsufficientFunds()
        {
            // Arrange
            var withdrawPayload = new
            {
                Amount = 1000
            };

            var content = new StringContent(JsonConvert.SerializeObject(withdrawPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Withdraw", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseString = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
            errorResponse.Should().NotBeNull();
            errorResponse.Title.Should().Be("Invalid withdrawal amount. There are insufficient funds.");
            errorResponse.Status.Should().Be(400);
        }

        public void Dispose()
        {
            ResetDatabase();
        }
    }
}