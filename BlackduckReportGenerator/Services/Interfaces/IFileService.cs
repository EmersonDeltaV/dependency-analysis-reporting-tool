using BlackduckReportGeneratorTool.Models.ServiceResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackduckReportGeneratorTool.Services.Interfaces
{
    public interface IFileService
    {
        ExtractResult ExtractFiles(string zipFilePath);

        bool TransferFiles(string sourcePath, string destinationPath);

        bool DeleteFile (string filePath);

        bool DeleteDirectory(string directoryPath);
    }
}
