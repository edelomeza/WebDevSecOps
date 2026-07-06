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

public class UsuarioApiClientTests
{
    // ========================================================================
    // GET /api/v1/Usuario?PageNumber=&PageSize=
    // ========================================================================

    [Fact]
    public async Task GetUsuarios_ShouldGetCorrectUrlWithQueryParams()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetUsuariosAsync(pageNumber: 2, pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario?PageNumber=2&PageSize=25", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetUsuarios_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetUsuariosAsync();

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetUsuarios_ShouldReturnPaginatedResponse_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetUsuariosAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(ContractTestData.ValidUsuario.StrNombre, result.Items[0].StrNombre);
    }

    [Fact]
    public async Task GetUsuarios_ShouldReturnEmptyList_WhenApiReturnsNoItems()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.EmptyPaginatedResponse, HttpStatusCode.OK);

        var result = await service.GetUsuariosAsync();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetUsuarios_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.GetUsuariosAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsuarios_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetUsuariosAsync();

        Assert.Null(result);
    }

    // ========================================================================
    // GET /api/v1/Usuario/buscar?texto=&PageNumber=&PageSize=
    // ========================================================================

    [Fact]
    public async Task BuscarUsuarios_ShouldGetCorrectUrlWithSearchTextAndPagination()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.BuscarUsuariosAsync("Juan", pageNumber: 2, pageSize: 25);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario/buscar?texto=Juan&PageNumber=2&PageSize=25", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.BuscarUsuariosAsync("test");

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldReturnPaginatedResponse_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidPaginatedResponse, HttpStatusCode.OK);

        var result = await service.BuscarUsuariosAsync("Juan");

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(ContractTestData.ValidUsuario.StrNombre, result.Items[0].StrNombre);
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldReturnEmptyList_WhenApiReturnsNoItems()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.EmptyPaginatedResponse, HttpStatusCode.OK);

        var result = await service.BuscarUsuariosAsync("NoExiste");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.BuscarUsuariosAsync("test");

        Assert.Null(result);
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.BuscarUsuariosAsync("test");

        Assert.Null(result);
    }

    [Fact]
    public async Task BuscarUsuarios_ShouldUrlEncodeSearchText()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.BuscarUsuariosAsync("María José & Cía.");

        var path = handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/');
        Assert.NotNull(path);
        Assert.Contains("api/v1/Usuario/buscar?texto=", path);
        var query = path.Substring(path.IndexOf('?') + 1);
        var pairs = query.Split('&');
        var textoPair = pairs.First(p => p.StartsWith("texto=", StringComparison.Ordinal));
        var textoValue = textoPair.Substring(6);
        Assert.Equal("Mar%C3%ADa%20Jos%C3%A9%20%26%20C%C3%ADa.", textoValue);
    }

    // ========================================================================
    // GET /api/v1/Usuario/{id}
    // ========================================================================

    [Fact]
    public async Task GetUsuarioById_ShouldGetCorrectUrl()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetUsuarioByIdAsync(42);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario/42", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task GetUsuarioById_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.GetUsuarioByIdAsync(1);

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetUsuarioById_ShouldReturnUsuario_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidUsuario, HttpStatusCode.OK);

        var result = await service.GetUsuarioByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(ContractTestData.ValidUsuario.Id, result.Id);
        Assert.Equal(ContractTestData.ValidUsuario.StrNombre, result.StrNombre);
        Assert.Equal(ContractTestData.ValidUsuario.StrCorreoElectronico, result.StrCorreoElectronico);
        Assert.Equal(ContractTestData.ValidUsuario.DteFechaRegistro, result.DteFechaRegistro);
    }

    [Fact]
    public async Task GetUsuarioById_ShouldReturnNull_WhenApiReturns404()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.NotFound);

        var result = await service.GetUsuarioByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsuarioById_ShouldReturnNull_WhenApiReturnsServerError()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.InternalServerError);

        var result = await service.GetUsuarioByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsuarioById_ShouldReturnNull_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.GetUsuarioByIdAsync(1);

        Assert.Null(result);
    }

    // ========================================================================
    // POST /api/v1/Usuario
    // ========================================================================

    [Fact]
    public async Task CreateUsuario_ShouldPostToCorrectEndpoint()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task CreateUsuario_ShouldSendCorrectJsonBody_WithSnakeCaseFields()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("strNombre", out _), "Body must contain 'strNombre' per contract");
        Assert.Equal("Nuevo Usuario", root.GetProperty("strNombre").GetString());

        Assert.True(root.TryGetProperty("strPWD", out _), "Body must contain 'strPWD' per contract");
        Assert.True(root.TryGetProperty("strCorreoElectronico", out _), "Body must contain 'strCorreoElectronico' per contract");
    }

    [Fact]
    public async Task CreateUsuario_ShouldSetContentTypeHeader()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.NotNull(handler.LastRequest?.Content);
        Assert.Equal("application/json", handler.LastRequest.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateUsuario_ShouldIncludeBearerToken()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.NotNull(handler.LastRequest?.Headers.Authorization);
        Assert.Equal(ContractTestData.TestToken, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task CreateUsuario_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.OK);

        var result = await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.FieldErrors);
    }

    [Fact]
    public async Task CreateUsuario_ShouldReturnFailureWithFieldErrors_WhenApiReturnsValidationError()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ValidationApiError, HttpStatusCode.BadRequest);

        var result = await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.False(result.Success);
        Assert.NotNull(result.FieldErrors);
        Assert.True(result.FieldErrors.ContainsKey("strNombre"));
        Assert.Contains("obligatorio", result.FieldErrors["strNombre"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateUsuario_ShouldReturnFailure_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    // ========================================================================
    // PUT /api/v1/Usuario/{id}
    // ========================================================================

    [Fact]
    public async Task UpdateUsuario_ShouldPutToCorrectEndpoint()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.UpdateUsuarioAsync(1, ContractTestData.ValidUpdateViewModel);

        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task UpdateUsuario_ShouldSendCorrectJsonBody_WithRowVersion()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.UpdateUsuarioAsync(1, ContractTestData.ValidUpdateViewModel);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("id", out _), "Body must contain 'id' per contract");
        Assert.Equal(1, root.GetProperty("id").GetInt32());

        Assert.True(root.TryGetProperty("strNombre", out _), "Body must contain 'strNombre' per contract");
        Assert.True(root.TryGetProperty("strCorreoElectronico", out _), "Body must contain 'strCorreoElectronico' per contract");
        Assert.True(root.TryGetProperty("rowVersion", out var rv), "Body must contain 'rowVersion' per contract");
        Assert.IsType<JsonElement>(rv);
    }

    [Fact]
    public async Task UpdateUsuario_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.OK);

        var result = await service.UpdateUsuarioAsync(1, ContractTestData.ValidUpdateViewModel);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateUsuario_ShouldReturnConflictMessage_WhenApiReturns409()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ConflictApiError, HttpStatusCode.Conflict);

        var result = await service.UpdateUsuarioAsync(1, ContractTestData.ValidUpdateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.ConflictMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateUsuario_ShouldReturnFailure_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.UpdateUsuarioAsync(1, ContractTestData.ValidUpdateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    // ========================================================================
    // DELETE /api/v1/Usuario/{id}
    // ========================================================================

    [Fact]
    public async Task DeleteUsuario_ShouldDeleteToCorrectEndpoint()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.DeleteUsuarioAsync(1, ContractTestData.ValidUsuario.RowVersion!);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
        Assert.Equal("api/v1/Usuario/1", handler.LastRequest?.RequestUri?.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task DeleteUsuario_ShouldSendCorrectJsonBody_WithIdAndRowVersion()
    {
        var (service, handler) = CreateServiceWithToken();

        await service.DeleteUsuarioAsync(1, ContractTestData.ValidUsuario.RowVersion!);

        Assert.NotNull(handler.LastRequestBody);
        using var doc = JsonDocument.Parse(handler.LastRequestBody);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("id", out _), "Body must contain 'id' per contract");
        Assert.Equal(1, root.GetProperty("id").GetInt32());

        Assert.True(root.TryGetProperty("rowVersion", out _), "Body must contain 'rowVersion' per contract");
    }

    [Fact]
    public async Task DeleteUsuario_ShouldReturnOk_WhenApiSucceeds()
    {
        var (service, _) = CreateServiceWithToken("", HttpStatusCode.OK);

        var result = await service.DeleteUsuarioAsync(1, ContractTestData.ValidUsuario.RowVersion!);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteUsuario_ShouldReturnConflictMessage_WhenApiReturns409()
    {
        var (service, _) = CreateServiceWithToken(ContractTestData.ConflictApiError, HttpStatusCode.Conflict);

        var result = await service.DeleteUsuarioAsync(1, ContractTestData.ValidUsuario.RowVersion!);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.ConflictMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteUsuario_ShouldReturnFailure_WhenMissingToken()
    {
        var (service, _) = CreateServiceWithoutToken();

        var result = await service.DeleteUsuarioAsync(1, ContractTestData.ValidUsuario.RowVersion!);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.MissingTokenMessage, result.ErrorMessage);
    }

    // ========================================================================
    // Resilience Contract
    // ========================================================================

    [Fact]
    public async Task CreateUsuario_NetworkError_ShouldReturnConnectionFailure()
    {
        var service = CreateServiceWithHandler(new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused")));

        var result = await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.ConnectionErrorMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUsuario_Cancellation_ShouldThrowOperationCanceledException()
    {
        var service = CreateServiceWithHandler(new ThrowingHttpMessageHandler(new OperationCanceledException()));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel));
    }

    [Fact]
    public async Task CreateUsuario_GenericException_ShouldReturnUnexpectedError()
    {
        var service = CreateServiceWithHandler(new ThrowingHttpMessageHandler(new InvalidOperationException("Unexpected")));

        var result = await service.CreateUsuarioAsync(ContractTestData.ValidCreateViewModel);

        Assert.False(result.Success);
        Assert.Equal(ContractTestData.UnexpectedErrorMessage, result.ErrorMessage);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static ClaimsPrincipal CreateAuthenticatedPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "admin"),
            new Claim("token_key", "stored-key")
        ], "test"));
    }

    private (UsuarioService Service, MockHttpMessageHandler Handler) CreateServiceWithToken(
        object? responseContent = null,
        HttpStatusCode? statusCode = null)
    {
        var mockHandler = new MockHttpMessageHandler(responseContent, statusCode ?? HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext { User = CreateAuthenticatedPrincipal() });

        var tokenStoreMock = new Mock<ITokenStore>();
        tokenStoreMock
            .Setup(x => x.GetTokenFromPrincipal(It.IsAny<ClaimsPrincipal>()))
            .Returns(ContractTestData.TestToken);

        var loggerMock = new Mock<ILogger<UsuarioService>>();
        var service = new UsuarioService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, mockHandler);
    }

    private UsuarioService CreateServiceWithHandler(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext { User = CreateAuthenticatedPrincipal() });

        var tokenStoreMock = new Mock<ITokenStore>();
        tokenStoreMock
            .Setup(x => x.GetTokenFromPrincipal(It.IsAny<ClaimsPrincipal>()))
            .Returns(ContractTestData.TestToken);

        var loggerMock = new Mock<ILogger<UsuarioService>>();
        return new UsuarioService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);
    }

    private (UsuarioService Service, MockHttpMessageHandler Handler) CreateServiceWithoutToken()
    {
        var handler = new MockHttpMessageHandler(null, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext { User = CreateAuthenticatedPrincipal() });

        var tokenStoreMock = new Mock<ITokenStore>();
        tokenStoreMock
            .Setup(x => x.GetTokenFromPrincipal(It.IsAny<ClaimsPrincipal>()))
            .Returns((string?)null);

        var loggerMock = new Mock<ILogger<UsuarioService>>();
        var service = new UsuarioService(httpClient, httpContextAccessorMock.Object, tokenStoreMock.Object, loggerMock.Object);

        return (service, handler);
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
