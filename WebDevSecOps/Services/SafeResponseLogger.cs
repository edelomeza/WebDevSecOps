using System.Text.RegularExpressions;

namespace WebDevSecOps.Services;

public static class SafeResponseLogger
{
    public static async Task LogResponseFailure(
        ILogger logger,
        HttpResponseMessage response,
        string operation,
        int? entityId = null,
        CancellationToken ct = default)
    {
        var statusCode = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(ct);
        var sanitized = SanitizeBody(body);

        if (entityId.HasValue)
            logger.LogWarning("{Operation} {EntityId} failed with status {StatusCode}",
                operation, entityId, statusCode);
        else
            logger.LogWarning("{Operation} failed with status {StatusCode}",
                operation, statusCode);

        logger.LogTrace("{Operation} response body: {Body}", operation, sanitized);
    }

    private static string SanitizeBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        if (body.Length > 500)
            body = body[..500] + "... (truncated)";

        body = Regex.Replace(body, @"eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", "[REDACTED]");
        body = Regex.Replace(body, @"""password""\s*:\s*""[^""]*""", @"""password"":""[REDACTED]""", RegexOptions.IgnoreCase);
        body = Regex.Replace(body, @"""strPWD""\s*:\s*""[^""]*""", @"""strPWD"":""[REDACTED]""", RegexOptions.IgnoreCase);
        body = Regex.Replace(body, @"""token""\s*:\s*""[^""]*""", @"""token"":""[REDACTED]""", RegexOptions.IgnoreCase);

        return body;
    }
}
