using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SAST;

public class GlobalSecurityTests
{
    [Fact]
    public async Task SecurityHeaders_CspHeaderIsPresent()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var response = await client.GetAsync("/");
        Assert.True(response.Headers.Contains("Content-Security-Policy"),
            "CSP header is missing");
    }

    [Fact]
    public async Task SecurityHeaders_XContentTypeOptionsIsPresent()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var response = await client.GetAsync("/");
        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header is missing");
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptionsIsPresent()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var response = await client.GetAsync("/");
        Assert.True(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header is missing");
    }

    [Fact]
    public async Task SecurityHeaders_ReferrerPolicyIsPresent()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var response = await client.GetAsync("/");
        Assert.True(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy header is missing");
    }

    [Fact]
    public void Hsts_IsConfiguredInProgramCs()
    {
        var projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var programPath = Path.Combine(projectRoot, "WebDevSecOps", "Program.cs");

        Assert.True(File.Exists(programPath), $"Program.cs not found at: {programPath}");

        var content = File.ReadAllText(programPath);
        Assert.Contains("UseHsts", content);
    }

    [Fact]
    public void HttpsRedirection_IsConfiguredInProgramCs()
    {
        var projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var programPath = Path.Combine(projectRoot, "WebDevSecOps", "Program.cs");

        Assert.True(File.Exists(programPath), $"Program.cs not found at: {programPath}");

        var content = File.ReadAllText(programPath);
        Assert.Contains("UseHttpsRedirection", content);
    }

    [Fact]
    public void Controllers_HaveAuthorizeAttribute()
    {
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Controller)) && !t.IsAbstract);

        foreach (var controller in controllers)
        {
            var hasAuthorize = controller.GetCustomAttribute<AuthorizeAttribute>() != null;
            var hasAllowAnonymous = controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;

            if (!hasAllowAnonymous)
            {
                Assert.True(hasAuthorize,
                    $"{controller.Name} should have [Authorize] attribute for security");
            }
        }
    }

    [Fact]
    public async Task NonexistentRoute_RedirectsToLoginWhenUnauthenticated()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var response = await client.GetAsync("/nonexistent-route");
        var location = response.Headers.Location?.ToString() ?? "";

        Assert.Equal((int)HttpStatusCode.Redirect, (int)response.StatusCode);
        Assert.Contains("/Login", location);
        Assert.Contains("ReturnUrl", location);
    }

    [Fact]
    public async Task RateLimiting_BlocksExcessiveRequests()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(client.GetAsync("/Login"));
        }

        var responses = await Task.WhenAll(tasks);
        var tooManyRequests = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        Assert.True(tooManyRequests > 0,
            "Rate limiting should block excessive requests (429 Too Many Requests)");
    }

    [Fact]
    public async Task CsrfProtection_PostWithoutTokenReturns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var formData = new Dictionary<string, string>
        {
            { "user", "testuser" },
            { "password", "Test@123!" },
        };
        var content = new FormUrlEncodedContent(formData);

        var response = await client.PostAsync("/Login", content);
        var body = await response.Content.ReadAsStringAsync();

        var snippet = body.Length > 300 ? body[..300] : body;
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400, got {(int)response.StatusCode}. Body: {snippet}");
    }
}
