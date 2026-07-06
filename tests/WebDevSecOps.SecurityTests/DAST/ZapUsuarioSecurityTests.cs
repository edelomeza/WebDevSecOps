using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebDevSecOps.SecurityTests.Common;
using WebDevSecOps.Services;

namespace WebDevSecOps.SecurityTests.DAST;

public class ZapUsuarioSecurityTests
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
    public async Task UsuarioCreateEndpoint_RejectsUnauthenticatedAccess()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Create");
        Assert.Equal((int)HttpStatusCode.Redirect, status);
    }

    [Fact]
    public async Task UsuarioCreateEndpoint_DoesNotExposeErrorDetails()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var (_, body) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Create/invalid");
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stack", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsuarioDeleteEndpoint_RejectsInvalidId()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Delete/99999");
        Assert.Equal((int)HttpStatusCode.Redirect, status);
    }

    [Fact]
    public async Task UsuarioEndpoint_RespondsWithinThreshold()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var sw = Stopwatch.StartNew();
        var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario");
        sw.Stop();

        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.True(sw.ElapsedMilliseconds < 60000,
            $"Usuarios page took {sw.ElapsedMilliseconds}ms (threshold: 60000ms)");
    }

    [Fact]
    public async Task UsuarioCreatePost_RejectsMalformedPayload()
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
            { "StrNombre", "'" },
            { "StrCorreoElectronico", "user@example.com" },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, _) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
    }

    [Fact]
    public async Task UsuarioCreatePost_RejectsLongInput()
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
            { "StrNombre", new string('A', 500) },
            { "StrCorreoElectronico", new string('A', 500) },
            { "StrPWD", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Usuario/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("50 caracteres", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsuarioEndpoints_PreventParameterPollution()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var url = "/Usuario/Create?StrNombre=real&StrNombre=fake";
        var (status, _) = await SecurityTestHelpers.GetAsync(client, url);
        Assert.Equal((int)HttpStatusCode.OK, status);
    }

    [Fact]
    public async Task UsuarioSearch_RespondsWithinThreshold()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var sw = Stopwatch.StartNew();
        var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Usuario/Index?searchString=Juan");
        sw.Stop();

        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.True(sw.ElapsedMilliseconds < 60000,
            $"Usuarios search took {sw.ElapsedMilliseconds}ms (threshold: 60000ms)");
    }

    [Fact]
    public async Task UsuarioSearch_RejectsLongSearchString()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var longSearch = new string('A', 500);
        var (status, body) = await SecurityTestHelpers.GetAsync(client, $"/Usuario/Index?searchString={longSearch}");
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains(longSearch, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsuarioSearch_PreventsParameterPollution()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthenticateAsync(client);

        var url = "/Usuario/Index?searchString=real&searchString=fake";
        var (status, _) = await SecurityTestHelpers.GetAsync(client, url);
        Assert.Equal((int)HttpStatusCode.OK, status);
    }
}
