namespace WebDevSecOps.SecurityTests.Common;

public static class SecurityTestData
{
    public static class XssPayloads
    {
        public const string BasicScript = "<script>alert('xss')</script>";
        public const string ImageOnError = "<img src=x onerror=alert(1)>";
        public const string SvgOnLoad = "<svg onload=alert(1)>";
        public const string BodyOnLoad = "<body onload=alert(1)>";
        public const string EncodedScript = "%3Cscript%3Ealert('xss')%3C/script%3E";
        public const string DoubleTag = "<<script>alert('xss')</script>";
        public const string IframeSrc = "<iframe src=javascript:alert(1)>";
        public const string LinkTag = "<link rel=stylesheet href=javascript:alert(1)>";

        public static IEnumerable<object[]> All =>
            new List<object[]>
            {
                new object[] { BasicScript },
                new object[] { ImageOnError },
                new object[] { SvgOnLoad },
                new object[] { BodyOnLoad },
                new object[] { EncodedScript },
                new object[] { IframeSrc },
            };
    }

    public static class SqlInjectionPayloads
    {
        public const string BasicSql = "' OR '1'='1";
        public const string UnionSql = "' UNION SELECT * FROM Users--";
        public const string DropTable = "'; DROP TABLE Usuarios; --";
        public const string BooleanBlind = "' OR '1'='1' --";
        public const string TimeBlind = "'; WAITFOR DELAY '00:00:05'; --";
        public const string StackedQuery = "'; SELECT * FROM sys.tables; --";
        public const string CommentSql = "admin'--";
        public const string OrSql = "' OR 1=1--";

        public static IEnumerable<object[]> All =>
            new List<object[]>
            {
                new object[] { BasicSql },
                new object[] { UnionSql },
                new object[] { DropTable },
                new object[] { BooleanBlind },
                new object[] { CommentSql },
                new object[] { OrSql },
            };
    }

    public static class OpenRedirectUrls
    {
        public const string ExternalHttp = "http://evil.com";
        public const string ExternalHttps = "https://evil.com";
        public const string ProtocolRelative = "//evil.com";
        public const string JavaScriptUrl = "javascript:alert(1)";
        public const string DataUrl = "data:text/html,<script>alert(1)</script>";
        public const string EvilSubdomain = "https://evil.localhost";
        public const string TabNab = "https://evil.com#@localhost";
        public const string DoubleSlashEvil = "//evil.com@localhost";

        public static IEnumerable<object[]> AllExternal =>
            new List<object[]>
            {
                new object[] { ExternalHttp },
                new object[] { ExternalHttps },
                new object[] { ProtocolRelative },
                new object[] { JavaScriptUrl },
                new object[] { DataUrl },
            };
    }

    public static class WeakPasswords
    {
        public const string TooShort = "Ab1";
        public const string NoUpper = "abcdef1!";
        public const string NoLower = "ABCDEF1!";
        public const string NoDigit = "Abcdefgh!";
        public const string NoSpecial = "Abcdefgh1";
        public const string Repeated = "AAAAAAAA!1";
        public const string Sequential = "Abcdefg!1";
        public const string CommonPassword = "Password1!";

        public static IEnumerable<object[]> All =>
            new List<object[]>
            {
                new object[] { TooShort },
                new object[] { NoUpper },
                new object[] { NoLower },
                new object[] { NoDigit },
                new object[] { NoSpecial },
            };
    }

    public static class ValidModels
    {
        public const string ValidEmail = "user@example.com";
        public const string ValidPassword = "FakePwdTest123!";
        public const string ValidNombre = "Juan Perez";
        public static readonly string ValidRowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    public static class MaliciousUsernames
    {
        public const string SqlInjection = "' OR 1=1--";
        public const string XssScript = "<script>alert(1)</script>";
        public const string PathTraversal = "../../etc/passwd";
        public const string NullByte = "user%00@example.com";
        public const string UnicodeNormalization = "\uFF55\uFF53\uFF45\uFF52"; // ｕｓｅｒ
        public static readonly string VeryLong = new string('A', 1000);
    }
}
