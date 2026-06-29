using System.Net;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SAST;

public class LoginSecurityTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LoginPage_RejectsEmptyUser(string? user)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Login");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "user", user ?? "" },
            { "password", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Login", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("required", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LoginPage_RejectsEmptyPassword(string? password)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Login");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "user", "testuser" },
            { "password", password ?? "" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Login", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("required", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.XssPayloads.All), MemberType = typeof(SecurityTestData.XssPayloads))]
    public async Task LoginPage_SanitizesXssInUserField(string xssPayload)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Login");

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "user", xssPayload },
            { "password", SecurityTestData.ValidModels.ValidPassword },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Login", form);
        Assert.Equal((int)HttpStatusCode.OK, status);

        Assert.DoesNotContain(xssPayload, body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.OpenRedirectUrls.AllExternal), MemberType = typeof(SecurityTestData.OpenRedirectUrls))]
    public async Task LoginPage_BlocksOpenRedirect(string returnUrl)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync($"/Login?returnUrl={HttpUtility.UrlEncode(returnUrl)}");
        Assert.NotEqual((int)HttpStatusCode.Redirect, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(body,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        var token = match.Success ? match.Groups[1].Value : "";

        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "user", "testuser" },
            { "password", "Test@123!" },
            { "returnUrl", returnUrl },
        });

        var postResponse = await client.PostAsync("/Login", form);
        var location = postResponse.Headers.Location?.ToString() ?? "";

        bool wouldRedirect = location.Contains(returnUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(wouldRedirect, $"Open redirect should be blocked for: {returnUrl}");
    }

    [Fact]
    public async Task LoginPage_ReturnsLoginPageOnGet()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var (status, body) = await SecurityTestHelpers.GetAsync(client, "/Login");
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("Ingresar", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_HasAntiForgeryToken()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/Login");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("__RequestVerificationToken", body);
    }

    [Fact]
    public async Task LoginPage_LocksOutAfterMultipleFailedAttempts()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/Login");

        for (int i = 0; i < 5; i++)
        {
            var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
            {
                { "__RequestVerificationToken", token },
                { "user", "testuser" },
                { "password", $"WrongPass{i}!" },
            });

            await client.PostAsync("/Login", form);
        }

        var finalForm = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "user", "testuser" },
            { "password", "Test@123!" },
        });

        var (status, _) = await SecurityTestHelpers.PostAsync(client, "/Login", finalForm);
        Assert.Equal((int)HttpStatusCode.TooManyRequests, status);
    }
}
