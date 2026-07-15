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

public class ProductoApiClientTests
{
    private static readonly byte[] _rowVersion = [0x01, 0x02, 0x03, 0x04];

    private (ProductoService Service, MockHttpMessageHandler Handler) CreateServiceWithToken(
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

        var loggerMock = new Mock<ILogger<ProductoService>>();
        var service = new ProductoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    private (ProductoService Service, MockHttpMessageHandler Handler) CreateServiceWithoutToken()
    {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestConstants.ApiBaseUrl) };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var tokenStoreMock = new Mock<ITokenStore>();

        var loggerMock = new Mock<ILogger<ProductoService>>();
        var service = new ProductoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    [Fact]
    public async Task GetProductos_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetProductosAsync(pageNumber: 2, pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Producto?PageNumber=2&PageSize=25", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetProductos_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetProductosAsync();

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetProductos_ShouldReturnData_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidProductoPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetProductosAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Laptop Gamer", result.Items[0].StrNombreProducto);
    }

    [Fact]
    public async Task GetProductos_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.GetProductosAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductos_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetProductosAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchProductos_ShouldBuildCorrectUrl_WithTexto()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchProductosAsync("Laptop", 1, 10);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("api/v1/Producto/buscar", url);
        Assert.Contains("texto=Laptop", url);
        Assert.Contains("PageNumber=1", url);
        Assert.Contains("PageSize=10", url);
    }

    [Fact]
    public async Task SearchProductos_ShouldBuildCorrectUrl_WithAllParams()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchProductosAsync("Teclado", 2, 20);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("texto=Teclado", url);
        Assert.Contains("PageNumber=2", url);
        Assert.Contains("PageSize=20", url);
    }

    [Fact]
    public async Task GetProductoById_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetProductoByIdAsync(42);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Producto/42", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetProductoById_ShouldReturnProducto_WhenFound()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidProducto, HttpStatusCode.OK);

        var result = await service.GetProductoByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Laptop Gamer", result.StrNombreProducto);
    }

    [Fact]
    public async Task GetProductoById_ShouldReturnNull_WhenNotFound()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.NotFound);

        var result = await service.GetProductoByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProducto_ShouldPostCorrectData()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateProductoAsync(ContractTestData.ValidProductoCreateViewModel);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Producto", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task CreateProducto_ShouldSerializeRequestBody()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateProductoAsync(ContractTestData.ValidProductoCreateViewModel);

        Assert.NotNull(handler.LastRequestBody);
        var json = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal("Nuevo Producto", json.RootElement.GetProperty("strNombreProducto").GetString());
        Assert.Equal(5, json.RootElement.GetProperty("intNumeroExistencia").GetInt32());
    }

    [Fact]
    public async Task CreateProducto_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.Created);

        var result = await service.CreateProductoAsync(ContractTestData.ValidProductoCreateViewModel);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateProducto_ShouldReturnFail_WhenApiReturnsError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ProductoValidationApiError, HttpStatusCode.BadRequest);

        var result = await service.CreateProductoAsync(ContractTestData.ValidProductoCreateViewModel);

        Assert.False(result.Success);
        Assert.NotNull(result.FieldErrors);
        Assert.Contains("strNombreProducto", result.FieldErrors.Keys);
    }

    [Fact]
    public async Task CreateProducto_ShouldReturnFail_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.CreateProductoAsync(ContractTestData.ValidProductoCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateProducto_ShouldPutCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.NoContent);

        await service.UpdateProductoAsync(1, ContractTestData.ValidProductoUpdateViewModel);

        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Producto/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateProducto_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.NoContent);

        var result = await service.UpdateProductoAsync(1, ContractTestData.ValidProductoUpdateViewModel);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateProducto_ShouldReturnConflict_WhenConcurrencyError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ProductoConflictApiError, HttpStatusCode.Conflict);

        var result = await service.UpdateProductoAsync(1, ContractTestData.ValidProductoUpdateViewModel);

        Assert.False(result.Success);
        Assert.Contains("modificado", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProducto_ShouldDeleteCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.OK);

        await service.DeleteProductoAsync(1, _rowVersion);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Producto/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task DeleteProducto_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.OK);

        var result = await service.DeleteProductoAsync(1, _rowVersion);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteProducto_ShouldReturnFail_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.DeleteProductoAsync(1, _rowVersion);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }
}
