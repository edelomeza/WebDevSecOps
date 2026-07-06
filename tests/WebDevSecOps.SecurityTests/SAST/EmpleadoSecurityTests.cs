using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebDevSecOps.SecurityTests.Common;
using WebDevSecOps.Services;

namespace WebDevSecOps.SecurityTests.SAST;

public class EmpleadoSecurityTests
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
    public async Task EmpleadoIndex_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Empleado/Index");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task EmpleadoCreate_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Empleado/Create");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task EmpleadoUpdate_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Empleado/Update/1");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task EmpleadoDelete_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Empleado/Delete/1");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EmpleadoCreate_RejectsEmptyNombre(string? nombre)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Empleado/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", nombre ?? "" },
            { "StrAPaterno", "" },
            { "StrAMaterno", "" },
            { "StrCURP", "" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Empleado/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("obligatorio", body);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.XssPayloads.All), MemberType = typeof(SecurityTestData.XssPayloads))]
    public async Task EmpleadoCreate_RejectsXssInNombre(string xssPayload)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Empleado/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", xssPayload },
            { "StrAPaterno", "" },
            { "StrAMaterno", "" },
            { "StrCURP", "" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Empleado/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("solo permite letras", body.ToLowerInvariant());
    }
}
