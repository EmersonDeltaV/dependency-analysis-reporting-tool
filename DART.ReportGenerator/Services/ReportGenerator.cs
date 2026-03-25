using ClosedXML.Excel;
using DART.Core.Models;
using DART.ReportGenerator.Interfaces;

namespace DART.ReportGenerator.Services;

public sealed class ReportGenerator : IReportGenerator
{
    public XLWorkbook BuildWorkbook(IReadOnlyCollection<RowDetails> rows, string productName, string productVersion, string productIteration)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Black Duck Security Risks");

        FormatHeader(worksheet, productName, productVersion, productIteration);

        var currentRow = 8;
        foreach (var row in rows)
        {
            worksheet.Cell(currentRow, 1).Value = row.ApplicationName;
            worksheet.Cell(currentRow, 2).Value = row.SoftwareComponent;
            worksheet.Cell(currentRow, 3).Value = row.Version;
            worksheet.Cell(currentRow, 4).Value = row.SecurityRisk;
            worksheet.Cell(currentRow, 5).Value = row.VulnerabilityId;
            worksheet.Cell(currentRow, 6).Value = row.RecommendedFix;
            worksheet.Cell(currentRow, 8).Value = row.MatchType;
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();
        return workbook;
    }

    public string BuildOutputFileName(string appCode, DateTime timestamp)
        => $"dart-summary-{appCode}-{timestamp:yyyy-MM-dd-HHmmss}.xlsx";

    public string GenerateCurrentFormatReport(IReadOnlyCollection<RowDetails> rows, string outputDirectory, string appCode, string productName, string productVersion, string productIteration)
    {
        Directory.CreateDirectory(outputDirectory);

        using var workbook = BuildWorkbook(rows, productName, productVersion, productIteration);
        var fileName = BuildOutputFileName(appCode, DateTime.Now);
        var outputPath = Path.Combine(outputDirectory, fileName);

        workbook.SaveAs(outputPath);

        return outputPath;
    }

    private static void FormatHeader(IXLWorksheet worksheet, string productName, string productVersion, string productIteration)
    {
        worksheet.Range(1, 1, 1, 11).Merge();
        worksheet.Range(2, 1, 2, 11).Merge();
        worksheet.Range(3, 1, 3, 11).Merge();

        worksheet.Cell(1, 1).Value = productName;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(2, 1).Value = productVersion;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(3, 1).Value = productIteration;
        worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var cellBefore = worksheet.Cell(4, 1);
        cellBefore.Value = "To be filled out before the review";
        cellBefore.Style.Fill.BackgroundColor = XLColor.Green;
        cellBefore.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var cellDuring = worksheet.Cell(5, 1);
        cellDuring.Value = "To be filled out during the review";
        cellDuring.Style.Fill.BackgroundColor = XLColor.Yellow;
        cellDuring.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var cellNew = worksheet.Cell(4, 2);
        cellNew.Value = "New Findings";
        cellNew.Style.Fill.BackgroundColor = XLColor.LightPink;
        cellNew.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var cellExisting = worksheet.Cell(5, 2);
        cellExisting.Value = "Existing Findings";
        cellExisting.Style.Fill.BackgroundColor = XLColor.LightBlue;
        cellExisting.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        worksheet.Range("A7:L7").SetAutoFilter();

        var headers = new[]
        {
            "Application",
            "Software Component",
            "Version",
            "Security Risk",
            "Vulnerability ID",
            "Recommended Fix Version",
            "Found in Previous Scan?",
            "Match Type",
            "Review with Cybersecurity Team?",
            "Action Plan",
            "Final Status / Work item",
            "Notes"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(7, i + 1).Value = headers[i];
        }

        for (var i = 1; i <= 7; i++)
        {
            var cell = worksheet.Cell(7, i);
            cell.Style.Fill.BackgroundColor = XLColor.Green;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        for (var i = 8; i <= 12; i++)
        {
            var cell = worksheet.Cell(7, i);
            cell.Style.Fill.BackgroundColor = XLColor.Yellow;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
    }
}
