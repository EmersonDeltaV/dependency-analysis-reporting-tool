using ClosedXML.Excel;
using DART.Core.Models;

namespace DART.ReportGenerator.Interfaces;

public interface IReportGenerator
{
    XLWorkbook BuildWorkbook(IReadOnlyCollection<RowDetails> rows, string productName, string productVersion, string productIteration);

    string BuildOutputFileName(string appCode, DateTime timestamp);

    string GenerateCurrentFormatReport(IReadOnlyCollection<RowDetails> rows, string outputDirectory, string appCode, string productName, string productVersion, string productIteration);
}
