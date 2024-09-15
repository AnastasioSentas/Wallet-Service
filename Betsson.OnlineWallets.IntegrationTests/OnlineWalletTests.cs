using System.Net;
using FluentAssertions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnlineWallets.IntegrationTests.Models;

using Betsson.OnlineWallets.Data;
using Betsson.OnlineWallets.IntegrationTests.Factories;
using Betsson.OnlineWallets.IntegrationTests.Helpers;
using Betsson.OnlineWallets.IntegrationTests.Models;

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
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
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
            var balanceResponse = await GetAndAssertResponseAsync<BalanceResponse>(response, HttpStatusCode.OK);
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
            var balanceResponse = await GetAndAssertResponseAsync<BalanceResponse>(response, HttpStatusCode.OK);
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
            var balanceResponse = await GetAndAssertResponseAsync<BalanceResponse>(response, HttpStatusCode.OK);
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
            var errorResponse = await GetAndAssertResponseAsync<ErrorResponse>(response, HttpStatusCode.BadRequest);
            errorResponse.Title.Should().Be("Invalid withdrawal amount. There are insufficient funds.");
            errorResponse.Status.Should().Be(400);
        }
        
        [Fact]
        public async Task Withdraw_Endpoint_ShouldHandleDecimalAmountsCorrectly()
        {
            // Arrange
            var depositPayload = new
            {
                Amount = 100.75m
            };

            var depositContent = new StringContent(JsonConvert.SerializeObject(depositPayload), Encoding.UTF8, "application/json");
            await _client.PostAsync("/OnlineWallet/Deposit", depositContent);

            var withdrawPayload = new
            {
                Amount = 50.25m
            };

            var withdrawContent = new StringContent(JsonConvert.SerializeObject(withdrawPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Withdraw", withdrawContent);

            // Assert
            var balanceResponse = await GetAndAssertResponseAsync<BalanceResponse>(response, HttpStatusCode.OK);
            balanceResponse.Amount.Should().Be(50.50m);
        }
        
        [Fact]
        public async Task Withdraw_Endpoint_ShouldReturnBadRequest_WhenNegativeAmount()
        {
            // Arrange
            var withdrawPayload = new
            {
                Amount = -50
            };

            var withdrawContent = new StringContent(JsonConvert.SerializeObject(withdrawPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Withdraw", withdrawContent);

            // Assert
            await response.AssertValidationErrorAsync(
                expectedTitle: "One or more validation errors occurred.",
                fieldName: "Amount",
                expectedErrorMessage: "'Amount' must be greater than or equal to '0'."
            );
        }
        
        [Fact]
        public async Task Deposit_Endpoint_ShouldReturnBadRequest_WhenMissingAmount()
        {
            // This test fails because the DepositRequest model currently uses a non-nullable decimal for the Amount field.
            // When an empty JSON object {} is sent in the request, the Amount field is automatically set to 0.
            // This prevents validation from failing, as 0 is considered a valid value.
            
            // Arrange
            var depositPayload = new {}; // No amount provided
            var depositContent = new StringContent(JsonConvert.SerializeObject(depositPayload), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/OnlineWallet/Deposit", depositContent);

            // Assert
            await response.AssertValidationErrorAsync(expectedTitle:"One or more validation errors occurred.",
                fieldName: "Amount",
                expectedErrorMessage: "Amount field should be greator or equal to 0.");
        }
        
        private async Task<T> GetAndAssertResponseAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            response.StatusCode.Should().Be(expectedStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var deserializedResponse = JsonConvert.DeserializeObject<T>(responseString);

            deserializedResponse.Should().NotBeNull();
            return deserializedResponse;
        }

        public void Dispose()
        {
            ResetDatabase();
        }
    }
}