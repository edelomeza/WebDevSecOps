using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using WebDevSecOps.SecurityTests.Common;

namespace WebDevSecOps.SecurityTests.SAST;

public class RoslynAnalyzerSetupTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string RulesetPath = Path.Combine(
        ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SAST", "roslyn-analyzers.ruleset");

    private const string SecurityCodeScanPackageId = "SecurityCodeScan.Vs2019";
    private const string SonarAnalyzerPackageId = "SonarAnalyzer.CSharp";

    [Fact]
    public void RulesetFile_Exists()
    {
        Assert.True(File.Exists(RulesetPath), $"Ruleset file not found at: {RulesetPath}");
    }

    [Fact]
    public void RulesetFile_HasErrorLevelRules()
    {
        var doc = XDocument.Load(RulesetPath);
        var rules = doc.Descendants("Rule")
            .Where(r => r.Attribute("Action")?.Value == "Error")
            .ToList();

        Assert.NotEmpty(rules);
        Assert.Contains(rules, r => r.Attribute("Id")?.Value == "S2077"); // SQL Injection
        Assert.Contains(rules, r => r.Attribute("Id")?.Value == "S5131"); // XSS
        Assert.Contains(rules, r => r.Attribute("Id")?.Value == "S5145"); // Open Redirect
        Assert.Contains(rules, r => r.Attribute("Id")?.Value == "SCS0001"); // SQL Injection (SCS)
    }

    [Fact]
    public void DirectoryBuildProps_HasSecurityAnalyzers()
    {
        var propsPath = Path.Combine(ProjectRoot, "Directory.Build.props");
        Assert.True(File.Exists(propsPath), "Directory.Build.props not found");

        var content = File.ReadAllText(propsPath);
        Assert.Contains(SecurityCodeScanPackageId, content);
        Assert.Contains(SonarAnalyzerPackageId, content);
    }

    [Fact]
    public void Project_HasInternalsVisibleToSecurityTests()
    {
        var internalsPath = Path.Combine(
            ProjectRoot, "WebDevSecOps", "Properties", "InternalsVisibleTo.cs");

        Assert.True(File.Exists(internalsPath));
        var content = File.ReadAllText(internalsPath);
        Assert.Contains("WebDevSecOps.SecurityTests", content);
    }

    [Fact]
    public void SonarCloudProperties_Exists()
    {
        var sonarPath = Path.Combine(
            ProjectRoot, "tests", "WebDevSecOps.SecurityTests", "SAST", ".sonarcloud.properties");

        Assert.True(File.Exists(sonarPath));
        var content = File.ReadAllText(sonarPath);
        Assert.Contains("sonar.projectKey", content);
        Assert.Contains("sonar.security.sast.roslyn.rulesetPath", content);
    }
}
