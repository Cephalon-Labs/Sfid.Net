using System.Globalization;
using System.Text;

namespace Sfid.Net;

internal static class SnowfakeApplicationIdentityResolver
{
    public static string ResolveApplicationName(string? applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return "app";

        var simpleName = applicationName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        return Sanitize(string.IsNullOrWhiteSpace(simpleName) ? applicationName : simpleName);
    }

    public static string ResolveInstanceId(string? configuredInstanceId, string applicationName)
    {
        if (!string.IsNullOrWhiteSpace(configuredInstanceId))
            return Sanitize(configuredInstanceId);

        var hostIdentity =
            Environment.GetEnvironmentVariable("POD_NAME") ??
            Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME") ??
            Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ??
            Environment.GetEnvironmentVariable("HOSTNAME") ??
            Environment.GetEnvironmentVariable("COMPUTERNAME") ??
            Environment.MachineName;

        return $"{applicationName}-{Sanitize(hostIdentity)}-{Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "instance";

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        }

        var sanitized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "instance" : sanitized;
    }
}
