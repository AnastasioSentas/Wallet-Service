using System.Net;
using Betsson.OnlineWallets.IntegrationTests.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace Betsson.OnlineWallets.IntegrationTests.Helpers;

public static class HttpResponseExtensions
{
    public static async Task AssertValidationErrorAsync(this HttpResponseMessage response, string expectedTitle, string fieldName, string expectedErrorMessage)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseString = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);

        errorResponse.Should().NotBeNull();
        errorResponse.Title.Should().Be(expectedTitle);
        errorResponse.Status.Should().Be(400);

        errorResponse.Errors.Should().ContainKey(fieldName);
        errorResponse.Errors[fieldName].Should().Contain(expectedErrorMessage);
    }
}