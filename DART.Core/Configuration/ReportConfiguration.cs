namespace DART.Core.Configuration;

public sealed class ReportConfiguration
{
    private string _outputFilePath = string.Empty;
    private string _logPath = string.Empty;

    public string OutputFilePath
    {
        get => Path.IsPathRooted(_outputFilePath)
            ? _outputFilePath
            : Path.Combine(Directory.GetCurrentDirectory(), _outputFilePath);
        set => _outputFilePath = value;
    }

    public string LogPath
    {
        get => Path.IsPathRooted(_logPath)
            ? _logPath
            : Path.Combine(Directory.GetCurrentDirectory(), _logPath);
        set => _logPath = value;
    }

    public string ProductName { get; set; } = string.Empty;

    public string ProductVersion { get; set; } = string.Empty;

    public string ProductIteration { get; set; } = string.Empty;
}
