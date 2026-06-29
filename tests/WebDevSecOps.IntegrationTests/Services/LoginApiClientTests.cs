using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Models;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.IntegrationTests.Services;

public class LoginApiClientTests
{
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    // ========================================================================
    // Request Contract: what the consumer (AuthService) sends to the provider
    // ========================================================================

    [Fact]
    public async Task LoginRequest_ShouldPostToCorrectEndpoint()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Login/login", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task LoginRequest_ShouldMapFieldsToCorrectJsonPropertyNames()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(new LoginRequest { Username = "admin", Password = "FakePass1!" });

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("user", out var userProp), "Body must contain 'user' per contract");
        Assert.Equal("admin", userProp.GetString());

        Assert.True(root.TryGetProperty("password", out var passProp), "Body must contain 'password' per contract");
        Assert.Equal("FakePass1!", passProp.GetString());
    }

    [Fact]
    public async Task LoginRequest_ShouldNotContainExtraUnexpectedFields()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;

        Assert.Equal(2, root.EnumerateObject().Count());
    }

    [Fact]
    public async Task LoginRequest_ShouldSetContentTypeToApplicationJson()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.NotNull(handler.LastRequest?.Content);
        var contentType = handler.LastRequest.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal("application/json", contentType.MediaType);
    }

    [Fact]
    public async Task LoginRequest_ShouldSendEmptyStringsForBlankValues()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(ContractTestData.LoginRequestWithEmptyValues);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal("", doc.RootElement.GetProperty("user").GetString());
        Assert.Equal("", doc.RootElement.GetProperty("password").GetString());
    }

    [Fact]
    public async Task LoginRequest_ShouldHandleSpecialCharactersCorrectly()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await service.LoginAsync(ContractTestData.LoginRequestWithSpecialChars);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;
        Assert.Equal("userñáéíóú", root.GetProperty("user").GetString());
        Assert.Equal("p@ss#W0rD!\"'&<>", root.GetProperty("password").GetString());
    }

    // ========================================================================
    // Response Contract: what the consumer expects from the provider
    // ========================================================================

    [Fact]
    public async Task LoginResponse_WithValidToken_ShouldReturnSuccess()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidLoginResponse, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContractTestData.ValidLoginResponse.Token, result.Token);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginResponse_WithExtraUnexpectedFields_ShouldStillReturnSuccess()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ResponseWithExtraFields, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal("valid-token-extra", result.Token);
    }

    [Fact]
    public async Task LoginResponse_TokenWithSpecialCharacters_ShouldReturnSuccess()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.LoginResponseWithSpecialCharsToken, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContractTestData.LoginResponseWithSpecialCharsToken.Token, result.Token);
    }

    [Fact]
    public async Task LoginResponse_WithNullToken_ShouldReturnFailure()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ResponseWithNullToken, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Respuesta inv\u00e1lida del servidor de autenticaci\u00f3n", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginResponse_WithEmptyToken_ShouldReturnFailure()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ResponseWithEmptyToken, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginResponse_MissingTokenField_ShouldReturnFailure()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ResponseMissingTokenField, HttpStatusCode.OK);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Respuesta inv\u00e1lida del servidor de autenticaci\u00f3n", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginResponse_NonJsonBodyOnError_ShouldReturnGenericFailure()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.InvalidJson, HttpStatusCode.InternalServerError);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Error inesperado. Intente nuevamente.", result.ErrorMessage);
    }

    // ========================================================================
    // Error Contract: verified error response shapes
    // ========================================================================

    [Fact]
    public async Task LoginError_Unauthorized_ShouldReturnFailureWithDetail()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.UnauthorizedError, HttpStatusCode.Unauthorized);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal(ContractTestData.UnauthorizedError.Detail, result.ErrorMessage);
    }

    [Fact]
    public async Task LoginError_BadRequest_ShouldReturnFailureWithDetail()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ValidationError, HttpStatusCode.BadRequest);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal(ContractTestData.ValidationError.Detail, result.ErrorMessage);
    }

    [Fact]
    public async Task LoginError_InternalServerError_WithoutBody_ShouldReturnGenericFailure()
    {
        var handler = new MockHttpMessageHandler(null, HttpStatusCode.InternalServerError);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Error inesperado. Intente nuevamente.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginError_WithTitleButNoDetail_ShouldFallbackToTitle()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ErrorWithMissingDetail, HttpStatusCode.BadRequest);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Custom error", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginError_WithEmptyTitleAndDetail_ShouldReturnEmptyString()
    {
        var handler = new MockHttpMessageHandler(ContractTestData.ErrorWithEmptyFields, HttpStatusCode.BadRequest);
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("", result.ErrorMessage);
    }

    // ========================================================================
    // Resilience Contract: handling network/provider failures
    // ========================================================================

    [Fact]
    public async Task LoginResilience_HttpRequestException_ShouldReturnConnectionFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("conexi\u00f3n", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginResilience_Cancellation_ShouldThrowOperationCanceledException()
    {
        var handler = new ThrowingHttpMessageHandler(new OperationCanceledException());
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.LoginAsync(ContractTestData.ValidLoginRequest));
    }

    [Fact]
    public async Task LoginResilience_GenericException_ShouldReturnGenericFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Unexpected failure"));
        var client = CreateHttpClient(handler);
        var service = new AuthService(client, _loggerMock.Object);

        var result = await service.LoginAsync(ContractTestData.ValidLoginRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal("Error inesperado. Intente nuevamente.", result.ErrorMessage);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
