using System.Net;
using System.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebDevSecOps.SecurityTests.Common;
using WebDevSecOps.Services;

namespace WebDevSecOps.SecurityTests.SAST;

public class UsuarioSecurityTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IAuthService>();
                    services.AddSingleton<IAuthService>(new FakeAuthService());

                    services.PostConfigure<CookieAuthenticationOptions>(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
                });
            });
    }

    private static (WebApplicationFactory<Program> Factory, HttpClient Client) CreateUnauthenticatedClient()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        return (factory, client);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Login");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "Input.Username", "admin" },
            { "Input.Password", "Admin@123!" },
        });

        await client.PostAsync("/Login", form);
    }

    [Fact]
    public async Task UsuarioCreate_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Create");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task UsuarioIndex_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task UsuarioUpdate_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Update/1");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task UsuarioDelete_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Delete/1");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UsuarioCreate_RejectsEmptyNombre(string? nombre)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Usuario/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", nombre ?? "" },
            { "StrCorreoElectronico", SecurityTestData.ValidModels.ValidEmail },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("obligatorio", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.XssPayloads.All), MemberType = typeof(SecurityTestData.XssPayloads))]
    public async Task UsuarioCreate_RejectsXssInNombre(string xssPayload)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Usuario/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", xssPayload },
            { "StrCorreoElectronico", SecurityTestData.ValidModels.ValidEmail },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);

        Assert.DoesNotContain(xssPayload, body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.SqlInjectionPayloads.All), MemberType = typeof(SecurityTestData.SqlInjectionPayloads))]
    public async Task UsuarioCreate_RejectsSqlInjectionInNombre(string sqlPayload)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Usuario/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", sqlPayload },
            { "StrCorreoElectronico", SecurityTestData.ValidModels.ValidEmail },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("permite", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.WeakPasswords.All), MemberType = typeof(SecurityTestData.WeakPasswords))]
    public async Task UsuarioCreate_RequiresStrongPassword(string weakPassword)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Usuario/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", "Test User" },
            { "StrCorreoElectronico", SecurityTestData.ValidModels.ValidEmail },
            { "StrPWD", weakPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("debe", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requiere API externa")]
    public async Task UsuarioCreate_RejectsDuplicateEmail()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Usuario/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", "Test User" },
            { "StrCorreoElectronico", "duplicate@example.com" },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("already", body, StringComparison.OrdinalIgnoreCase);
    }
}
