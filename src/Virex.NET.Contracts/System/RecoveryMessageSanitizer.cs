using System.Text.RegularExpressions;

namespace Virex.NET.Contracts;

public static class RecoveryMessageSanitizer
{
    private const int MaximumLength = 512;
    private const int EllipsisLength = 3;
    private static readonly Regex SensitiveValuePattern = new Regex(
        @"(?<key>password|passwd|secret|token|credential|authorization|api[-_]?key)\s*[:=]\s*\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex PathPattern = new Regex(
        @"(?:(?:[A-Za-z]:[\\/])|(?:\\\\)|(?:/))[^\s,;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? Sanitize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var sanitized = message!.Trim().Replace("\r", " ").Replace("\n", " ");
        var stackTraceMarker = sanitized.IndexOf(" at ", StringComparison.Ordinal);
        if (stackTraceMarker >= 0)
            sanitized = sanitized.Substring(0, stackTraceMarker);

        sanitized = SensitiveValuePattern.Replace(sanitized, "${key}=[redacted]");
        sanitized = PathPattern.Replace(sanitized, "[path]");
        sanitized = string.Concat(sanitized.Select(character =>
            char.IsControl(character) ? ' ' : character));

        return sanitized.Length <= MaximumLength
            ? sanitized
            : sanitized.Substring(0, MaximumLength - EllipsisLength) + "...";
    }
}
