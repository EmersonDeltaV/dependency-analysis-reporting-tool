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
        Task AddEOLAnalysisSheetAsync(IXLWorkbook workbook, List<EOLPackageData> eolData);
        IXLWorkbook GetWorkbook();
        void SaveWorkbook(IXLWorkbook workbook);
    }
}