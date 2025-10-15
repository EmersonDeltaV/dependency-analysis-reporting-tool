using DART.BlackduckAnalysis.Models;
using DART.BlackduckAnalysis.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DART.BlackduckAnalysis.Services.Implementation
{
    public class FileService(ILogger<FileService> logger) : IFileService
    {
        private readonly ILogger _logger = logger;

        public bool DeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                    _logger.LogInformation("Directory deleted: " + directoryPath);
                    return true;
                }
                else
                {
                    _logger.LogInformation("Directory not found: " + directoryPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the directory:");
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: " + filePath);
                    return true;
                }
                else
                {
                    _logger.LogError("File not found: " + filePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the file:");
                _logger.LogError(ex.Message);
                return false;
            }
        }

        public ExtractResult ExtractFiles(string zipFilePath)
        {
            try
            {
                string zipDirectory = Path.GetDirectoryName(zipFilePath) ?? string.Empty;

                // Create a new directory for extraction in the same folder as the ZIP file
                string extractPath = Path.Combine(zipDirectory, Path.GetFileNameWithoutExtension(zipFilePath));


                // Check if the ZIP file exists
                if (File.Exists(zipFilePath))
                {
                    // Extract the contents of the ZIP file
                    ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                    _logger.LogInformation("Extraction complete. Files extracted to: " + extractPath);
                    return new ExtractResult(extractPath);
                }
                else
                {
                    _logger.LogError("ZIP file not found: " + zipFilePath);
                    return new ExtractResult(string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during extraction
                _logger.LogError("An error occurred while extracting the ZIP file:");
                _logger.LogError(ex.Message);
                return new ExtractResult(string.Empty);
            }

        }

        public bool TransferFiles(string sourcePath, string destinationPath)
        {
            try
            {
                // Ensure the source directory exists
                if (!Directory.Exists(sourcePath))
                {
                    _logger.LogError("Source directory not found: " + sourcePath);
                    return false;
                }

                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Get all CSV files from the source directory and its subdirectories
                var csvFiles = Directory.GetFiles(sourcePath, "*.csv", SearchOption.AllDirectories);

                foreach (var file in csvFiles)
                {
                    // Determine the destination file path
                    var fileName = Path.GetFileName(file);
                    var destinationFilePath = Path.Combine(destinationPath, fileName);

                    // Copy the file to the destination directory
                    File.Copy(file, destinationFilePath, true);
                    File.Delete(file);
                }

                _logger.LogInformation("File transfer complete. Files transferred to: " + destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during file transfer
                _logger.LogError("An error occurred while transferring files:");
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
