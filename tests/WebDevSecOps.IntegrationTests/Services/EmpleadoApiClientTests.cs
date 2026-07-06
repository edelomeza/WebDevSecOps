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

public class EmpleadoApiClientTests
{
    private static readonly byte[] _rowVersion = [0x01, 0x02, 0x03, 0x04];

    private (EmpleadoService Service, MockHttpMessageHandler Handler) CreateServiceWithToken(
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

        var loggerMock = new Mock<ILogger<EmpleadoService>>();
        var service = new EmpleadoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    private (EmpleadoService Service, MockHttpMessageHandler Handler) CreateServiceWithoutToken()
    {
        var mockHandler = new MockHttpMessageHandler(null, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestConstants.ApiBaseUrl) };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var tokenStoreMock = new Mock<ITokenStore>();

        var loggerMock = new Mock<ILogger<EmpleadoService>>();
        var service = new EmpleadoService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    [Fact]
    public async Task GetEmpleados_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetEmpleadosAsync(pageNumber: 2, pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Empleado?PageNumber=2&PageSize=25", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetEmpleados_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetEmpleadosAsync();

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetEmpleados_ShouldReturnData_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidEmpleadoPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetEmpleadosAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Carlos Lopez", result.Items[0].StrNombre);
    }

    [Fact]
    public async Task GetEmpleados_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.GetEmpleadosAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmpleados_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetEmpleadosAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchEmpleados_ShouldBuildCorrectUrl_WithTexto()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchEmpleadosAsync("Carlos", null, 1, 10);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("api/v1/Empleado/buscar", url);
        Assert.Contains("texto=Carlos", url);
        Assert.Contains("PageNumber=1", url);
        Assert.Contains("PageSize=10", url);
    }

    [Fact]
    public async Task SearchEmpleados_ShouldBuildCorrectUrl_WithIdTipoEmpleado()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchEmpleadosAsync(null, 2, 1, 10);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("api/v1/Empleado/buscar", url);
        Assert.Contains("idTipoEmpleado=2", url);
    }

    [Fact]
    public async Task SearchEmpleados_ShouldBuildCorrectUrl_WithAllFilters()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.SearchEmpleadosAsync("Maria", 3, 2, 20);

        var url = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.Contains("texto=Maria", url);
        Assert.Contains("idTipoEmpleado=3", url);
        Assert.Contains("PageNumber=2", url);
        Assert.Contains("PageSize=20", url);
    }

    [Fact]
    public async Task GetEmpleadoById_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetEmpleadoByIdAsync(42);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Empleado/42", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetEmpleadoById_ShouldReturnEmpleado_WhenFound()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidEmpleado, HttpStatusCode.OK);

        var result = await service.GetEmpleadoByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Carlos Lopez", result.StrNombre);
    }

    [Fact]
    public async Task GetEmpleadoById_ShouldReturnNull_WhenNotFound()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.NotFound);

        var result = await service.GetEmpleadoByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateEmpleado_ShouldPostCorrectData()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateEmpleadoAsync(ContractTestData.ValidEmpleadoCreateViewModel);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Empleado", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task CreateEmpleado_ShouldSerializeRequestBody()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.Created);

        await service.CreateEmpleadoAsync(ContractTestData.ValidEmpleadoCreateViewModel);

        Assert.NotNull(handler.LastRequestBody);
        var json = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal("Nuevo Empleado", json.RootElement.GetProperty("strNombre").GetString());
    }

    [Fact]
    public async Task CreateEmpleado_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.Created);

        var result = await service.CreateEmpleadoAsync(ContractTestData.ValidEmpleadoCreateViewModel);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateEmpleado_ShouldReturnFail_WhenApiReturnsError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.EmpleadoValidationApiError, HttpStatusCode.BadRequest);

        var result = await service.CreateEmpleadoAsync(ContractTestData.ValidEmpleadoCreateViewModel);

        Assert.False(result.Success);
        Assert.NotNull(result.FieldErrors);
        Assert.Contains("strNombre", result.FieldErrors.Keys);
    }

    [Fact]
    public async Task CreateEmpleado_ShouldReturnFail_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.CreateEmpleadoAsync(ContractTestData.ValidEmpleadoCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateEmpleado_ShouldPutCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.NoContent);

        await service.UpdateEmpleadoAsync(1, ContractTestData.ValidEmpleadoUpdateViewModel);

        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Empleado/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateEmpleado_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.NoContent);

        var result = await service.UpdateEmpleadoAsync(1, ContractTestData.ValidEmpleadoUpdateViewModel);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateEmpleado_ShouldReturnConflict_WhenConcurrencyError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.EmpleadoConflictApiError, HttpStatusCode.Conflict);

        var result = await service.UpdateEmpleadoAsync(1, ContractTestData.ValidEmpleadoUpdateViewModel);

        Assert.False(result.Success);
        Assert.Contains("modificado", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteEmpleado_ShouldDeleteCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken(null, HttpStatusCode.OK);

        await service.DeleteEmpleadoAsync(1, _rowVersion);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Empleado/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task DeleteEmpleado_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(null, HttpStatusCode.OK);

        var result = await service.DeleteEmpleadoAsync(1, _rowVersion);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteEmpleado_ShouldReturnFail_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.DeleteEmpleadoAsync(1, _rowVersion);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }
}
