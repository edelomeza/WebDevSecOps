using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security.Claims;
using WebDevSecOps.Models;
using WebDevSecOps.Pages;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.UnitTests;

public class LoginTest
{
    private readonly Mock<ILogger<AuthService>> _authServiceLoggerMock = new();

    private static (LoginModel PageModel, Mock<IAuthenticationService> AuthServiceMock, Mock<ITokenStore> TokenStoreMock) CreateLoginModel(
        IAuthService authService)
    {
        var loggerMock = new Mock<ILogger<LoginModel>>();
        var tokenStoreMock = new Mock<ITokenStore>();
        var pageModel = new LoginModel(authService, tokenStoreMock.Object, loggerMock.Object);

        var authServiceMock = new Mock<IAuthenticationService>();

        var services = new ServiceCollection();
        services.AddSingleton(authServiceMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        pageModel.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary()),
            ActionDescriptor = new CompiledPageActionDescriptor()
        };

        return (pageModel, authServiceMock, tokenStoreMock);
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

    // ========================================================================
    // LoginModel.OnPostAsync Tests
    // ========================================================================

    [Fact]
    public async Task OnPostAsync_ReturnsRedirect_WhenLoginSucceeds()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Success("valid-token"));

        var (pageModel, authServiceMock, tokenStoreMock) = CreateLoginModel(authMock.Object);

        tokenStoreMock
            .Setup(x => x.StoreToken("valid-token"))
            .Returns("stored-key");

        pageModel.Input = new LoginRequest
        {
            Username = "admin",
            Password = "pass"
        };

        var capturedPrincipal = default(ClaimsPrincipal);
        authServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>(
                (_, _, principal, _) => capturedPrincipal = principal)
            .Returns(Task.CompletedTask);

        var result = await pageModel.OnPostAsync(default);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);

        Assert.NotNull(capturedPrincipal);
        Assert.True(capturedPrincipal.Identity?.IsAuthenticated);
        Assert.Equal("admin", capturedPrincipal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("stored-key", capturedPrincipal.FindFirst("token_key")?.Value);

        tokenStoreMock.Verify(x => x.StoreToken("valid-token"), Times.Once);
        authServiceMock.Verify(x => x.SignInAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenLoginFails()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Failure("Credenciales inválidas."));

        var (pageModel, authServiceMock, _) = CreateLoginModel(authMock.Object);

        pageModel.Input = new LoginRequest
        {
            Username = "bad",
            Password = "wrong"
        };

        var result = await pageModel.OnPostAsync(default);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Credenciales inválidas.", pageModel.ErrorMessage);

        authServiceMock.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenNetworkError()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Failure(
                "Error de conexión con el servidor. Verifique su conexión e intente nuevamente."));

        var (pageModel, authServiceMock, _) = CreateLoginModel(authMock.Object);

        pageModel.Input = new LoginRequest
        {
            Username = "admin",
            Password = "pass"
        };

        var result = await pageModel.OnPostAsync(default);

        Assert.IsType<PageResult>(result);
        Assert.Contains("conexión", pageModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        authServiceMock.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var authMock = new Mock<IAuthService>();
        var (pageModel, authServiceMock, _) = CreateLoginModel(authMock.Object);

        pageModel.ModelState.AddModelError("Input.Username", "Required");

        pageModel.Input = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        var result = await pageModel.OnPostAsync(default);

        Assert.IsType<PageResult>(result);
        Assert.Null(pageModel.ErrorMessage);

        authMock.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        authServiceMock.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_RedirectsToReturnUrl_WhenLoginSucceedsWithLocalUrl()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Success("valid-token"));

        var (pageModel, authServiceMock, tokenStoreMock) = CreateLoginModel(authMock.Object);

        tokenStoreMock
            .Setup(x => x.StoreToken(It.IsAny<string>()))
            .Returns("stored-key");

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(x => x.IsLocalUrl("/dashboard"))
            .Returns(true);

        pageModel.Url = urlHelper.Object;
        pageModel.Input = new LoginRequest
        {
            Username = "admin",
            Password = "pass"
        };
        pageModel.ReturnUrl = "/dashboard";

        authServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var result = await pageModel.OnPostAsync(default);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_RedirectsToIndex_WhenLoginSucceedsWithNonLocalUrl()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Success("valid-token"));

        var (pageModel, authServiceMock, tokenStoreMock) = CreateLoginModel(authMock.Object);

        tokenStoreMock
            .Setup(x => x.StoreToken(It.IsAny<string>()))
            .Returns("stored-key");

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(x => x.IsLocalUrl("https://evil.com/phish"))
            .Returns(false);

        pageModel.Url = urlHelper.Object;
        pageModel.Input = new LoginRequest
        {
            Username = "admin",
            Password = "pass"
        };
        pageModel.ReturnUrl = "https://evil.com/phish";

        authServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var result = await pageModel.OnPostAsync(default);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenLoginFailsWithReturnUrl()
    {
        var authMock = new Mock<IAuthService>();
        authMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.Failure("Credenciales inválidas."));

        var (pageModel, authServiceMock, _) = CreateLoginModel(authMock.Object);

        pageModel.Input = new LoginRequest
        {
            Username = "bad",
            Password = "wrong"
        };
        pageModel.ReturnUrl = "/dashboard";

        var result = await pageModel.OnPostAsync(default);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Credenciales inválidas.", pageModel.ErrorMessage);

        authServiceMock.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    // ========================================================================
    // AuthService.LoginAsync Tests
    // ========================================================================

    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WhenApiReturnsToken()
    {
        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var response = new LoginResponse { Token = expectedToken, ExpiresIn = 3600 };

        var handler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedToken, result.Token);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenApiReturnsUnauthorized()
    {
        var handler = new MockHttpMessageHandler(
            new ErrorResponse { Title = "Unauthorized", Detail = "Credenciales inválidas" },
            HttpStatusCode.Unauthorized);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "bad", Password = "data" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Credenciales", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenApiReturnsEmptyToken()
    {
        var response = new LoginResponse { Token = string.Empty, ExpiresIn = 0 };

        var handler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenApiReturnsNullToken()
    {
        var response = new LoginResponse { Token = null!, ExpiresIn = 0 };

        var handler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Respuesta inválida del servidor de autenticación", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_OnHttpException()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.ServiceUnavailable);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        var handler = new ThrowingHttpMessageHandler(new OperationCanceledException());
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" }));
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_OnGenericException()
    {
        var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Unexpected error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Error inesperado. Intente nuevamente.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenErrorResponseMissingDetail()
    {
        var handler = new MockHttpMessageHandler(
            new ErrorResponse { Title = "Custom error", Detail = "" },
            HttpStatusCode.BadRequest);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Custom error", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenErrorResponseHasNoFields()
    {
        var handler = new MockHttpMessageHandler(
            new ErrorResponse { Title = "", Detail = "" },
            HttpStatusCode.BadRequest);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.Equal("", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WhenErrorResponseIsInvalidJson()
    {
        var handler = new MockHttpMessageHandler("not-json-at-all", HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "pass" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Error inesperado. Intente nuevamente.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_SendsCorrectPayloadToApi()
    {
        var response = new LoginResponse { Token = "token123", ExpiresIn = 3600 };

        var handler = new MockHttpMessageHandler(response, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        await service.LoginAsync(new LoginRequest { Username = "miUsuario", Password = "miPassword" });

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("\"user\":\"miUsuario\"", handler.LastRequestBody);
        Assert.Contains("\"password\":\"miPassword\"", handler.LastRequestBody);
    }

    // ========================================================================
    // AuthService.LogoutAsync Tests
    // ========================================================================

    [Fact]
    public async Task LogoutAsync_ReturnsTrue_OnSuccess()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LogoutAsync("some-token");

        Assert.True(result);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("Bearer some-token", handler.LastRequest?.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task LogoutAsync_ReturnsFalse_OnApiError()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LogoutAsync("some-token");

        Assert.False(result);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsFalse_OnException()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(TestConstants.ApiBaseUrl)
        };

        var service = new AuthService(httpClient, _authServiceLoggerMock.Object);
        var result = await service.LogoutAsync("some-token");

        Assert.False(result);
    }
}
