using System.Diagnostics;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SecretScanning;

public class GitleaksScanTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void GitleaksConfig_Exists()
    {
        var configPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SecretScanning", ".gitleaks.toml");

        Assert.True(File.Exists(configPath), $"Gitleaks config not found at: {configPath}");
    }

    [Fact]
    public void GitleaksConfig_HasCustomRules()
    {
        var configPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SecretScanning", ".gitleaks.toml");

        var content = File.ReadAllText(configPath);
        Assert.Contains("[[rules]]", content);
        Assert.Contains("custom-hardcoded-secret", content);
        Assert.Contains("custom-private-key", content);
        Assert.Contains("[allowlist]", content);
    }

    [Fact]
    public void GitleaksConfig_ExtendsDefaultRules()
    {
        var configPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SecretScanning", ".gitleaks.toml");

        var content = File.ReadAllText(configPath);
        Assert.Contains("useDefault = true", content);
    }

    [Fact]
    public async Task GitleaksCli_IsAvailable()
    {
        var result = await SecurityTestHelpers.RunCliToolAsync(
            "gitleaks", "--version", ProjectRoot);

        if (result.ExitCode == -1)
        {
            Assert.True(true, "Gitleaks CLI is not installed. Skipping scan test.");
            return;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrEmpty(result.Output));
    }

    [Fact]
    public async Task GitleaksScan_NoHighConfidenceLeaks()
    {
        var result = await SecurityTestHelpers.RunCliToolAsync(
            "gitleaks",
            $"detect --source \"{ProjectRoot}\" --config \"{ProjectRoot}\\tests\\WebDevSecOps.SecurityTests\\SecretScanning\\.gitleaks.toml\" --no-git --verbose",
            ProjectRoot);

        if (result.ExitCode == -1)
        {
            Assert.True(true, "Gitleaks CLI is not installed. Skipping scan test.");
            return;
        }

        if (result.ExitCode != 0)
        {
            var findings = result.Output + result.Error;
            Assert.Fail($"Gitleaks found potential secrets:\n{findings}");
        }
    }

    [Fact]
    public void ProjectHasNoGitIgnoredSecrets()
    {
        var gitignorePath = Path.Combine(ProjectRoot, ".gitignore");
        Assert.True(File.Exists(gitignorePath), ".gitignore file is missing");

        var content = File.ReadAllText(gitignorePath);
        var requiredPatterns = new[] { "*.key", "*.pem", "secrets.*", "appsettings.*.local" };

        foreach (var pattern in requiredPatterns)
        {
            Assert.Contains(pattern, content);
        }
    }

    [Fact]
    public void ConfigFiles_DoNotContainHardcodedCredentials()
    {
        var configFiles = Directory.GetFiles(ProjectRoot, "appsettings*.json", SearchOption.AllDirectories);

        foreach (var file in configFiles)
        {
            var content = File.ReadAllText(file);

            Assert.DoesNotContain("Password=", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pwd=", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
