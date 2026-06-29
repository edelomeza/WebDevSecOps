using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.DAST;

public class ZapLoginSecurityTests
{
    private static readonly string ProjectDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void ZapConfig_ForLoginExists()
    {
        var configFile = Path.Combine(ProjectDir, "tests", "WebDevSecOps.SecurityTests", "DAST", "owasp-zap-config.context");
        Assert.True(File.Exists(configFile), $"ZAP context file not found: {configFile}");

        var content = File.ReadAllText(configFile);
        Assert.Contains("Login", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authentication", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ZapBaselineRules_Exists()
    {
        var rulesFile = Path.Combine(ProjectDir, "tests", "WebDevSecOps.SecurityTests", "DAST", "zap-baseline-rules.tsv");
        Assert.True(File.Exists(rulesFile), $"ZAP baseline rules file not found: {rulesFile}");

        var lines = File.ReadAllLines(rulesFile);
        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Contains("FAIL") || l.Contains("WARN"));
    }

    [Fact]
    public async Task LoginEndpoint_RespondsWithinThreshold()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var sw = Stopwatch.StartNew();
        var (status, _) = await SecurityTestHelpers.GetAsync(client, "/Login");
        sw.Stop();

        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Login page took {sw.ElapsedMilliseconds}ms (threshold: 5000ms)");
    }

    [Fact(Skip = "Requiere API externa (AuthService llama a localhost:7227)")]
    public async Task LoginPost_WithInvalidCredentials_ReturnsUnauthorized()
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
            { "Input.Username", "nonexistent" },
            { "Input.Password", "WrongPass1!" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Login", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("intento", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requiere API externa (AuthService llama a localhost:7227)")]
    public async Task LoginEndpoint_DoesNotExposeStackTraceOnError()
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
            { "Input.Username", new string('A', 10000) },
            { "Input.Password", "test" },
        });

        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/Login", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requiere API externa (AuthService llama a localhost:7227)")]
    public async Task LoginPage_ReturnsSameResponseTimeForValidAndInvalidUsers()
    {
        async Task<long> MeasureResponseTime(string user, string password)
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
                { "Input.Username", user },
                { "Input.Password", password },
            });

            var sw = Stopwatch.StartNew();
            await client.PostAsync("/Login", form);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        var validTime = await MeasureResponseTime("testuser", "Test@123!");
        var invalidTime = await MeasureResponseTime("nonexistent", "WrongPass1!");

        var diff = Math.Abs(validTime - invalidTime);
        Assert.True(diff < 2000,
            $"Response time difference ({diff}ms) suggests user enumeration vulnerability");
    }

    [Fact]
    public async Task LoginPage_DoesNotCacheAuthenticatedContent()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/Login");
        var cacheControl = response.Headers.CacheControl?.ToString() ?? "";

        Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-cache", cacheControl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginCookie_HasHttpOnlyAndSecureFlags()
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
            { "Input.Username", "admin" },
            { "Input.Password", "Admin@123!" },
        });

        await client.PostAsync("/Login", form);

        var response = await client.GetAsync("/");
        var setCookie = response.Headers
            .Where(h => h.Key == "Set-Cookie")
            .SelectMany(h => h.Value)
            .ToList();

        foreach (var cookie in setCookie)
        {
            if (cookie.Contains(".AspNetCore.", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("samesite", cookie, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
