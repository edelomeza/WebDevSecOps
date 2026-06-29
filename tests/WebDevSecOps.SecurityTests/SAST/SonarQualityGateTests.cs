using System.Xml.Linq;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SAST;

public class SonarQualityGateTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Ruleset_HasAllOwasTop10Coverage()
    {
        var rulesetPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SAST", "roslyn-analyzers.ruleset");

        var doc = XDocument.Load(rulesetPath);
        var ruleIds = doc.Descendants("Rule")
            .Select(r => r.Attribute("Id")?.Value)
            .Where(id => id != null)
            .ToHashSet();

        var owaspTopTenRules = new Dictionary<string, string>
        {
            { "A01-BrokenAccessControl", "S4502" },
            { "A02-CryptographicFailures", "S4790" },
            { "A03-Injection", "S2077" },
            { "A07-IdentityAuthFailures", "S5145" },
            { "A08-SoftwareDataIntegrity", "S5773" },
            { "A05-SecurityMisconfiguration", "S3330" },
            { "A03-Injection-XSS", "S5131" },
        };

        foreach (var (category, ruleId) in owaspTopTenRules)
        {
            Assert.True(
                ruleIds.Contains(ruleId),
                $"Missing rule {ruleId} for OWASP Top 10 category {category}");
        }
    }

    [Fact]
    public void Ruleset_ErrorRulesUseRecommendedSeverity()
    {
        var rulesetPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SAST", "roslyn-analyzers.ruleset");

        var doc = XDocument.Load(rulesetPath);

        var nonErrorRules = doc.Descendants("Rule")
            .Where(r => r.Attribute("Action")?.Value != "Error")
            .ToList();

        Assert.False(
            nonErrorRules.Any(r => r.Attribute("Id")?.Value is "S2077" or "S5131" or "S5145"),
            "Critical security rules must be configured as 'Error', not lower severity");
    }

    [Fact]
    public void SonarProperties_ReferencesCorrectPaths()
    {
        var sonarPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SAST", ".sonarcloud.properties");

        var lines = File.ReadAllLines(sonarPath);

        Assert.Contains(lines, l => l.StartsWith("sonar.sources=WebDevSecOps"));
        Assert.Contains(lines, l => l.StartsWith("sonar.tests=tests"));
    }

    [Fact]
    public void NuGetAudit_IsEnabledInBuildProps()
    {
        var propsPath = Path.Combine(ProjectRoot, "Directory.Build.props");
        var content = File.ReadAllText(propsPath);

        Assert.Contains("<NuGetAudit>true</NuGetAudit>", content);
        Assert.Contains("<NuGetAuditMode>all</NuGetAuditMode>", content);
        Assert.Contains("<NuGetAuditLevel>low</NuGetAuditLevel>", content);
    }
}
