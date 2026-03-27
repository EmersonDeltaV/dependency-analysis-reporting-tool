namespace DART.Core;

public sealed class ReportConfiguration
{
    private string _outputFilePath = string.Empty;
    private string _logPath = string.Empty;

    public string OutputFilePath
    {
        get => ResolveConfiguredPath(_outputFilePath);
        set => _outputFilePath = value ?? string.Empty;
    }

    public string LogPath
    {
        get => ResolveConfiguredPath(_logPath);
        set => _logPath = value ?? string.Empty;
    }

    public string ProductName { get; set; } = string.Empty;

    public string ProductVersion { get; set; } = string.Empty;

    public string ProductIteration { get; set; } = string.Empty;

    private static string ResolveConfiguredPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return string.Empty;

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
    }
}
