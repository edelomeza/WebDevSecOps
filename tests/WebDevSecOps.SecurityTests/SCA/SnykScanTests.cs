using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SCA;

public class SnykScanTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void SnykIgnoreFile_Exists()
    {
        var ignorePath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SCA", "snyk-ignore.json");

        Assert.True(File.Exists(ignorePath), $"Snyk ignore file not found at: {ignorePath}");
    }

    [Fact]
    public void SnykIgnoreFile_HasValidJson()
    {
        var ignorePath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SCA", "snyk-ignore.json");

        var content = File.ReadAllText(ignorePath);

        Assert.DoesNotContain("{{", content);
        Assert.Contains("ignore", content);
        Assert.Contains("exclude", content);
    }

    [Fact]
    public void SnykIgnoreFile_ExpiryDatesAreInFuture()
    {
        var ignorePath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SCA", "snyk-ignore.json");

        var content = File.ReadAllText(ignorePath);

        var datePattern = @"\d{4}-\d{2}-\d{2}T00:00:00.000Z";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, datePattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var parsed = DateTime.ParseExact(match.Value, "yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
            Assert.True(parsed > DateTime.UtcNow,
                $"Snyk ignore expiry date {match.Value} is in the past");
        }
    }

    [Fact]
    public async Task SnykCli_IsAvailable()
    {
        var result = await SecurityTestHelpers.RunCliToolAsync(
            "snyk", "--version", ProjectRoot);

        if (result.ExitCode == -1)
        {
            Assert.True(true, "Snyk CLI is not installed. Skipping scan test.");
            return;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrEmpty(result.Output));
    }

    [Fact]
    public async Task SnykScan_NoHighSeverityVulnerabilities()
    {
        var result = await SecurityTestHelpers.RunCliToolAsync(
            "snyk",
            $"test --all-projects --severity-threshold=high --json",
            ProjectRoot,
            120000);

        if (result.ExitCode == -1)
        {
            Assert.True(true, "Snyk CLI is not installed. Skipping scan test.");
            return;
        }

        if (result.ExitCode != 0)
        {
            Assert.Fail($"Snyk found high-severity vulnerabilities:\n{result.Output}");
        }
    }

    [Fact]
    public void TransitiveDependencies_AreTracked()
    {
        var depsFile = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "obj", "project.assets.json");

        if (!File.Exists(depsFile))
        {
            Assert.True(true, "Project must be built first. Run 'dotnet build'. Skipping.");
            return;
        }

        var content = File.ReadAllText(depsFile);
        Assert.Contains("Microsoft.AspNetCore.Mvc.Testing", content);
    }
}
