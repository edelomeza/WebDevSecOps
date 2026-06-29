using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.IntegrationTests.Services;

public class LogoutApiClientTests
{
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    // ========================================================================
    // Request Contract: what the consumer (AuthService) sends to the provider
    // ========================================================================

    [Fact]
    public async Task LogoutRequest_ShouldPostToCorrectEndpoint()
    {
        var (service, handler) = CreateService();

        await service.LogoutAsync(ContractTestData.TestToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Logout/logout", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task LogoutRequest_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateService();

        await service.LogoutAsync(ContractTestData.TestToken);

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task LogoutRequest_ShouldNotIncludeRequestBody()
    {
        var (service, handler) = CreateService();

        await service.LogoutAsync(ContractTestData.TestToken);

        Assert.Null(handler.LastRequest?.Content);
    }

    // ========================================================================
    // Response Contract: handling of provider responses
    // ========================================================================

    [Fact]
    public async Task LogoutResponse_200Ok_ShouldReturnTrue()
    {
        var (service, _) = CreateService("", HttpStatusCode.OK);

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.True(result);
    }

    [Fact]
    public async Task LogoutResponse_201Created_ShouldReturnTrue()
    {
        var (service, _) = CreateService("", HttpStatusCode.Created);

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.True(result);
    }

    [Fact]
    public async Task LogoutResponse_401Unauthorized_ShouldReturnFalse()
    {
        var (service, _) = CreateService("", HttpStatusCode.Unauthorized);

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.False(result);
    }

    [Fact]
    public async Task LogoutResponse_500InternalServerError_ShouldReturnFalse()
    {
        var (service, _) = CreateService("", HttpStatusCode.InternalServerError);

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.False(result);
    }

    [Fact]
    public async Task LogoutResponse_404NotFound_ShouldReturnFalse()
    {
        var (service, _) = CreateService("", HttpStatusCode.NotFound);

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.False(result);
    }

    // ========================================================================
    // Contract Violations
    // ========================================================================

    [Fact]
    public async Task LogoutRequest_EmptyToken_ShouldSendBearerWithEmptyParameter()
    {
        var (service, handler) = CreateService("", HttpStatusCode.OK);

        await service.LogoutAsync("");

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal("", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task LogoutRequest_TokenWithSpecialCharacters_ShouldIncludeInHeader()
    {
        var (service, handler) = CreateService("", HttpStatusCode.OK);
        var specialToken = "tok+en/with=special!chars@and#symbols";

        await service.LogoutAsync(specialToken);

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal($"Bearer {specialToken}", handler.LastRequest.Headers.Authorization.ToString());
    }

    // ========================================================================
    // Resilience Contract
    // ========================================================================

    [Fact]
    public async Task LogoutResilience_NetworkError_ShouldReturnFalse()
    {
        var service = CreateServiceWithHandler(new ThrowingHttpMessageHandler(new HttpRequestException("Connection reset")));

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.False(result);
    }

    [Fact]
    public async Task LogoutResilience_GenericException_ShouldReturnFalse()
    {
        var service = CreateServiceWithHandler(new ThrowingHttpMessageHandler(new InvalidOperationException("Unexpected")));

        var result = await service.LogoutAsync(ContractTestData.TestToken);

        Assert.False(result);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private (AuthService Service, MockHttpMessageHandler Handler) CreateService(
        object? responseContent = null,
        HttpStatusCode? statusCode = null)
    {
        var handler = new MockHttpMessageHandler(responseContent, statusCode ?? HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _loggerMock.Object);
        return (service, handler);
    }

    private AuthService CreateServiceWithHandler(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        return new AuthService(httpClient, _loggerMock.Object);
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;
        public ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
