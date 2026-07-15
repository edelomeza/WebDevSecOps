using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Models;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.IntegrationTests.Services;

public class VentaApiClientTests
{
    private (VentaService Service, MockHttpMessageHandler Handler) CreateServiceWithToken(
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

        var loggerMock = new Mock<ILogger<VentaService>>();
        var service = new VentaService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    private (VentaService Service, MockHttpMessageHandler Handler) CreateServiceWithoutToken()
    {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestConstants.ApiBaseUrl) };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var tokenStoreMock = new Mock<ITokenStore>();

        var loggerMock = new Mock<ILogger<VentaService>>();
        var service = new VentaService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    [Fact]
    public async Task GetVentas_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetVentasAsync(pageNumber: 2, pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Venta?PageNumber=2&PageSize=25", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task SearchVentas_ShouldBuildCorrectUrl_WithTexto()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchVentasAsync("V-000001", null, null, 1, 10);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("api/v1/Venta/buscar", url);
        Assert.Contains("strClaveVenta=V-000001", url);
        Assert.Contains("strNombreCliente=V-000001", url);
    }

    [Fact]
    public async Task SearchVentas_ShouldBuildCorrectUrl_WithFechas()
    {
        var (service, handler) = CreateServiceWithToken();
        var desde = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

        await service.SearchVentasAsync(null, desde, hasta, 1, 10);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("dteFechaInicio=2026-07-01", url);
        Assert.Contains("dteFechaFin=2026-07-31", url);
    }

    [Fact]
    public async Task CreateVenta_ShouldPostCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateVentaAsync(ContractTestData.ValidVentaCreateViewModel);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Venta", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task CreateVenta_ShouldSerializeRequestBody()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateVentaAsync(ContractTestData.ValidVentaCreateViewModel);

        Assert.NotNull(handler.LastRequestBody);
        var json = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal(1, json.RootElement.GetProperty("idCliCliente").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("idSegUsuario").GetInt32());
    }

    [Fact]
    public async Task CreateVenta_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.Created);

        var result = await service.CreateVentaAsync(ContractTestData.ValidVentaCreateViewModel);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateVenta_ShouldReturnFail_WhenApiReturnsError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.VentaValidationApiError, HttpStatusCode.BadRequest);

        var result = await service.CreateVentaAsync(ContractTestData.ValidVentaCreateViewModel);

        Assert.False(result.Success);
        Assert.NotNull(result.FieldErrors);
        Assert.Contains("idCliCliente", result.FieldErrors.Keys);
    }

    [Fact]
    public async Task CreateVenta_ShouldReturnFail_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.CreateVentaAsync(ContractTestData.ValidVentaCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task GetVentas_ShouldReturnData_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidVentaPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetVentasAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("V-000001", result.Items[0].StrClaveVenta);
    }

    [Fact]
    public async Task GetVentas_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.GetVentasAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVentas_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetVentasAsync();

        Assert.Null(result);
    }
}
