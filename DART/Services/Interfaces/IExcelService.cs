using DART.Models;
using DART.EOLAnalysis;
using ClosedXML.Excel;

namespace DART.Services.Interfaces
{
    public interface IExcelService
    {
        void CompareExcelFiles(string filePath1, string filePath2, string outputFilePath);
        void PopulateRow(RowDetails rowDetails);
        void SaveReport();
        void AddEOLAnalysisSheet(IXLWorkbook workbook, List<PackageData> eolData);
        void AddEOLAnalysisSheet(IXLWorkbook workbook, List<PackageData> eolData, string sheetName);
        IXLWorkbook GetWorkbook();
        void SaveWorkbook(IXLWorkbook workbook);
    }
}
