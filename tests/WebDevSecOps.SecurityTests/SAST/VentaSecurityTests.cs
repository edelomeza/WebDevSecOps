using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebDevSecOps.SecurityTests.Common;
using WebDevSecOps.Services;

namespace WebDevSecOps.SecurityTests.SAST;

public class VentaSecurityTests
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
    public async Task VentaIndex_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Venta/Index");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task VentaCreate_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Venta/Create");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task VentaCreate_RejectsEmptyIdCliCliente(string? id)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Venta/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "IdCliCliente", id ?? "" },
            { "IdSegUsuario", "1" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Venta/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("obligatorio", body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task VentaCreate_RejectsEmptyIdSegUsuario(string? id)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Venta/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "IdCliCliente", "1" },
            { "IdSegUsuario", id ?? "" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Venta/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("obligatorio", body);
    }

    [Fact]
    public async Task VentaCreate_RejectsInvalidIdCliCliente()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Venta/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "IdCliCliente", "not-a-number" },
            { "IdSegUsuario", "1" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Venta/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("not valid", body.ToLowerInvariant());
    }

    [Fact]
    public async Task VentaCreate_RejectsInvalidIdSegUsuario()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Venta/Create");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "IdCliCliente", "1" },
            { "IdSegUsuario", "not-a-number" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Venta/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("not valid", body.ToLowerInvariant());
    }

    [Fact]
    public async Task ClientesAutocomplete_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Venta/ClientesAutocomplete?texto=Maria");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Fact]
    public async Task UsuariosAutocomplete_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory)
        using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Venta/UsuariosAutocomplete?texto=admin");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }
}
