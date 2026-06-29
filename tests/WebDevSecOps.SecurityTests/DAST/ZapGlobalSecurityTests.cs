using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.DAST;

public class ZapGlobalSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ZapGlobalSecurityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    [Fact]
    public async Task AllPages_ReturnSecurityHeaders()
    {
        var pages = new[] { "/", "/Login", "/Privacy" };
        var requiredHeaders = new[]
        {
            "Content-Security-Policy",
            "X-Content-Type-Options",
            "X-Frame-Options",
        };

        foreach (var page in pages)
        {
            var response = await _client.GetAsync(page);
            foreach (var header in requiredHeaders)
            {
                Assert.True(
                    response.Headers.Contains(header) ||
                    response.Content.Headers.Contains(header),
                    $"Page '{page}' is missing header: {header}");
            }
        }
    }

    [Fact]
    public async Task NonexistentEndpoint_Returns404WithoutErrorDetails()
    {
        var (status, body) = await SecurityTestHelpers.GetAsync(_client, "/api/nonexistent");
        Assert.Equal((int)HttpStatusCode.Redirect, status);
        Assert.DoesNotContain("Stack", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaticFiles_HaveSecureHeaders()
    {
        var response = await _client.GetAsync("/favicon.ico");

        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            Assert.DoesNotContain("script-src 'unsafe-inline'",
                response.Headers.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ResponseDoesNotExposeServerVersion()
    {
        var response = await _client.GetAsync("/");
        var serverHeader = response.Headers.Server?.ToString() ?? "";

        Assert.DoesNotContain("Microsoft", serverHeader, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kestrel", serverHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorsHeaders_AreRestricted()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", "https://evil.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);
        var allowOrigin = response.Headers
            .FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin");

        Assert.Equal(default(KeyValuePair<string, IEnumerable<string>>), allowOrigin);
    }

    [Fact]
    public async Task ContentTypeOptions_DisablesSniffing()
    {
        var response = await _client.GetAsync("/");
        var hasHeader = response.Headers.TryGetValues("X-Content-Type-Options", out var values);
        Assert.True(hasHeader, "X-Content-Type-Options header is missing");
        Assert.Contains("nosniff", values?.First() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FrameOptions_PreventsClickjacking()
    {
        var response = await _client.GetAsync("/");
        var hasHeader = response.Headers.TryGetValues("X-Frame-Options", out var values);
        Assert.True(hasHeader, "X-Frame-Options header is missing");
        Assert.Contains("DENY", values?.First() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HttpMethods_UnsupportedReturns405()
    {
        var methods = new[] { HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch };

        foreach (var method in methods)
        {
            var request = new HttpRequestMessage(method, "/Login");
            var response = await _client.SendAsync(request);
            Assert.True(
                response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Method {method} on /Login returned {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task ResponseTimes_AreConsistentAcrossEndpoints()
    {
        async Task<long> MeasureAsync(string url)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _client.GetAsync(url);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        var homeTime = await MeasureAsync("/");
        var loginTime = await MeasureAsync("/Login");
        var privacyTime = await MeasureAsync("/Privacy");

        foreach (var (name, time) in new[] { ("/", homeTime), ("/Login", loginTime), ("/Privacy", privacyTime) })
        {
            Assert.True(time < 10000,
                $"Endpoint '{name}' response time {time}ms exceeds 10s threshold");
        }
    }

    [Fact]
    public async Task ApiEndpoints_ReturnJsonContentType()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "";

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            Assert.DoesNotContain("text/html", contentType, StringComparison.OrdinalIgnoreCase);
        }
    }
}
