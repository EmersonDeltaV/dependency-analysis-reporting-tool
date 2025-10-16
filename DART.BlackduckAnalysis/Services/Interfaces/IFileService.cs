namespace DART.BlackduckAnalysis
{
    public interface IFileService
    {
        ExtractResult ExtractFiles(string zipFilePath);

        bool TransferFiles(string sourcePath, string destinationPath);

        bool DeleteFile (string filePath);

        bool DeleteDirectory(string directoryPath);
    }
}
