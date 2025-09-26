using BlackduckReportAnalysis.Models;
using DART.EOLAnalysis.Models;
using ClosedXML.Excel;

namespace BlackduckReportAnalysis
{
    public interface IExcelService
    {
        void CompareExcelFiles(string filePath1, string filePath2, string outputFilePath);
        void PopulateRow(RowDetails rowDetails);
        void SaveReport();
        void AddEOLAnalysisSheet(IXLWorkbook workbook, List<PackageData> eolData);
        IXLWorkbook GetWorkbook();
        void SaveWorkbook(IXLWorkbook workbook);
    }
}