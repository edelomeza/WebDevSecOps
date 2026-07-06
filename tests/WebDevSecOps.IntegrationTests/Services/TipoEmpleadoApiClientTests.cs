using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Models;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.IntegrationTests.Services;

public class TipoEmpleadoApiClientTests
{
    private (TipoEmpleadoService Service, MockHttpMessageHandler Handler) CreateServiceWithToken(
        object? responseContent = null, HttpStatusCode? statusCode = null)
    {
        var mockHandler = new MockHttpMessageHandler(responseContent, statusCode ?? HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestConstants.ApiBaseUrl) };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("token_key", "test-key"),
            new Claim(ClaimTypes.Name, "test-user")
        ], "test"));
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var tokenStoreMock = new Mock<ITokenStore>();
        tokenStoreMock.Setup(x => x.GetTokenFromPrincipal(It.IsAny<ClaimsPrincipal>()))
                      .Returns(ContractTestData.TestToken);

        var loggerMock = new Mock<ILogger<TipoEmpleadoService>>();
        var service = new TipoEmpleadoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    private (TipoEmpleadoService Service, MockHttpMessageHandler Handler) CreateServiceWithoutToken()
    {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestConstants.ApiBaseUrl) };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var tokenStoreMock = new Mock<ITokenStore>();

        var loggerMock = new Mock<ILogger<TipoEmpleadoService>>();
        var service = new TipoEmpleadoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    [Fact]
    public async Task GetAllAsync_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetAllAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/TipoEmpleado?PageNumber=1&PageSize=100", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetAllAsync_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetAllAsync();

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnData_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidTipoEmpleadoPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetAllAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Administrativo", result.Items[0].StrValor);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetAllAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetByIdAsync(5);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/TipoEmpleado/5", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnData_WhenFound()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidTipoEmpleado, HttpStatusCode.OK);

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Administrativo", result.StrValor);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.NotFound);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetByIdAsync(1);

        Assert.Null(result);
    }
}
