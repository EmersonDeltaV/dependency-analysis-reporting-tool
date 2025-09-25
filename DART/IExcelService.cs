using BlackduckReportAnalysis.Models;

namespace BlackduckReportAnalysis
{
    public interface IExcelService
    {
        void CompareExcelFiles(string filePath1, string filePath2, string outputFilePath);
        void PopulateRow(RowDetails rowDetails);
        void SaveReport();
    }
}