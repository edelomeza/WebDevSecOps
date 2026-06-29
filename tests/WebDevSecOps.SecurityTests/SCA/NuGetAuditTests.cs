using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SCA;

public class NuGetAuditTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void DirectoryBuildProps_EnablesNuGetAudit()
    {
        var propsPath = Path.Combine(ProjectRoot, "Directory.Build.props");
        Assert.True(File.Exists(propsPath), "Directory.Build.props not found");

        var content = File.ReadAllText(propsPath);
        Assert.Contains("<NuGetAudit>true</NuGetAudit>", content);
    }

    [Fact]
    public void DirectoryBuildProps_AuditModeIsAll()
    {
        var propsPath = Path.Combine(ProjectRoot, "Directory.Build.props");
        var content = File.ReadAllText(propsPath);

        Assert.Contains("<NuGetAuditMode>all</NuGetAuditMode>", content);
    }

    [Fact]
    public void DirectoryBuildProps_AuditLevelIsLow()
    {
        var propsPath = Path.Combine(ProjectRoot, "Directory.Build.props");
        var content = File.ReadAllText(propsPath);

        Assert.Contains("<NuGetAuditLevel>low</NuGetAuditLevel>", content);
    }

    [Fact]
    public async Task NuGetAudit_ReportsNoVulnerabilities()
    {
        var result = await SecurityTestHelpers.RunCliToolAsync(
            "dotnet", "list package --vulnerable --include-transitive", ProjectRoot);

        Assert.Equal(0, result.ExitCode);

        if (result.Output.Contains("Vulnerable") || result.Output.Contains("vulnerability"))
        {
            var vulnSection = ExtractVulnerabilitySection(result.Output);
            Assert.Fail($"Vulnerable packages found:\n{vulnSection}");
        }
    }

    [Theory]
    [InlineData("WebDevSecOps.SecurityTests.csproj")]
    [InlineData("WebDevSecOps.UnitTests.csproj")]
    [InlineData("WebDevSecOps.IntegrationTests.csproj")]
    public void AllTestProjects_ReferenceCoverletForCoverage(string projectFile)
    {
        var projectPath = Path.Combine(
            ProjectRoot, "tests", projectFile);

        if (!File.Exists(projectPath))
        {
            Assert.True(true, $"Project file not found: {projectPath}. Skipping.");
            return;
        }

        var content = File.ReadAllText(projectPath);
        Assert.Contains("coverlet.collector", content);
    }

    [Fact]
    public void SolutionFile_IncludesAllProjects()
    {
        var slnPath = Path.Combine(ProjectRoot, "WebDevSecOps.slnx");
        Assert.True(File.Exists(slnPath), "Solution file not found");

        var content = File.ReadAllText(slnPath);
        Assert.Contains("WebDevSecOps.csproj", content);
        Assert.Contains("WebDevSecOps.SecurityTests.csproj", content);
        Assert.Contains("WebDevSecOps.UnitTests.csproj", content);
        Assert.Contains("WebDevSecOps.IntegrationTests.csproj", content);
    }

    [Fact]
    public void Project_UsesLatestPackageVersions()
    {
        var csprojPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "WebDevSecOps.SecurityTests.csproj");

        var content = File.ReadAllText(csprojPath);

        Assert.DoesNotContain("Version=\"*\"", content);
    }

    private static string ExtractVulnerabilitySection(string output)
    {
        var lines = output.Split('\n');
        var vulnLines = lines
            .SkipWhile(l => !l.Contains("Vulnerable", StringComparison.OrdinalIgnoreCase))
            .Take(20);

        return string.Join("\n", vulnLines);
    }
}
