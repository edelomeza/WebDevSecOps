using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebDevSecOps.SecurityTests.Common;

public static class SecurityTestHelpers
{
    public static HttpClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>();
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    public static async Task<(int StatusCode, string Body)> GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, body);
    }

    public static async Task<(int StatusCode, string Body)> PostAsync(
        HttpClient client, string url, HttpContent content)
    {
        var response = await client.PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, body);
    }

    public static StringContent ToJsonPayload(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    public static FormUrlEncodedContent ToFormPayload(IDictionary<string, string> formData)
    {
        return new FormUrlEncodedContent(formData);
    }

    public static async Task<(int ExitCode, string Output, string Error)> RunCliToolAsync(
        string fileName, string arguments, string workingDirectory, int timeoutMs = 60000)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (-1, "", "CLI tool not found");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (process.WaitForExit(timeoutMs))
        {
            return (process.ExitCode, await outputTask, await errorTask);
        }

        process.Kill();
        return (-1, await outputTask, "TIMEOUT: " + await errorTask);
    }

    public static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || url.Length == 0)
            return false;

        if (url[0] == '/')
        {
            return url.Length == 1 || url[1] != '/';
        }

        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.IsLoopback;
    }

    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(body,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!match.Success)
            throw new InvalidOperationException(
                $"Anti-Forgery token not found in response from {url}");
        return match.Groups[1].Value;
    }
}
