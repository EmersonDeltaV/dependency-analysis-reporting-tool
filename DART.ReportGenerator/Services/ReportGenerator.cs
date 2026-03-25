using ClosedXML.Excel;
using DART.Core.Contracts;
using DART.Core.Models;
using DART.ReportGenerator.Interfaces;

namespace DART.ReportGenerator.Services;

public sealed class ReportGenerator : IReportGenerator
{
    private readonly WorkbookComparisonService _comparisonService;

    public ReportGenerator()
        : this(new WorkbookComparisonService())
    {
    }

    public ReportGenerator(WorkbookComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

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
        return GenerateCurrentFormatReport(
            rows,
            Array.Empty<EolFinding>(),
            outputDirectory,
            appCode,
            productName,
            productVersion,
            productIteration);
    }

    public string GenerateCurrentFormatReport(
        IReadOnlyCollection<RowDetails> rows,
        IReadOnlyCollection<EolFinding> eolFindings,
        string outputDirectory,
        string appCode,
        string productName,
        string productVersion,
        string productIteration)
    {
        Directory.CreateDirectory(outputDirectory);

        using var workbook = BuildWorkbook(rows, productName, productVersion, productIteration);

        if (eolFindings.Count > 0)
        {
            AddEolAnalysisSheet(workbook, eolFindings);
        }

        var fileName = BuildOutputFileName(appCode, DateTime.Now);
        var outputPath = Path.Combine(outputDirectory, fileName);

        workbook.SaveAs(outputPath);

        return outputPath;
    }

    public void CompareCurrentWithPrevious(string currentReportPath, string previousReportPath)
    {
        using var currentWorkbook = new XLWorkbook(currentReportPath);
        using var previousWorkbook = new XLWorkbook(previousReportPath);

        var currentWorksheet = currentWorkbook.Worksheet(1);
        var previousWorksheet = previousWorkbook.Worksheet(1);

        _comparisonService.ApplyComparison(currentWorksheet, previousWorksheet, 8);
        currentWorkbook.Save();
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

    private static void AddEolAnalysisSheet(XLWorkbook workbook, IReadOnlyCollection<EolFinding> eolFindings)
    {
        var eolWorksheet = workbook.Worksheets.Add("EOL Analysis");

        eolWorksheet.Cell(1, 1).Value = "Package ID";
        eolWorksheet.Cell(1, 2).Value = "Repository";
        eolWorksheet.Cell(1, 3).Value = "Project";
        eolWorksheet.Cell(1, 4).Value = "Current Version";
        eolWorksheet.Cell(1, 5).Value = "Version Date";
        eolWorksheet.Cell(1, 6).Value = "Age (Days)";
        eolWorksheet.Cell(1, 7).Value = "Latest Version";
        eolWorksheet.Cell(1, 8).Value = "Latest Version Date";
        eolWorksheet.Cell(1, 9).Value = "License";
        eolWorksheet.Cell(1, 10).Value = "License URL";
        eolWorksheet.Cell(1, 11).Value = "Recommended Action";

        eolWorksheet.Row(1).Style.Font.Bold = true;
        eolWorksheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
        eolWorksheet.Row(1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var row = 2;
        foreach (var item in eolFindings)
        {
            eolWorksheet.Cell(row, 1).Value = item.PackageId;
            eolWorksheet.Cell(row, 2).Value = item.Repository;
            eolWorksheet.Cell(row, 3).Value = item.Project;
            eolWorksheet.Cell(row, 4).Value = item.CurrentVersion;
            eolWorksheet.Cell(row, 5).Value = item.VersionDate;
            eolWorksheet.Cell(row, 6).Value = item.AgeDays;
            eolWorksheet.Cell(row, 7).Value = item.LatestVersion;
            eolWorksheet.Cell(row, 8).Value = item.LatestVersionDate;
            eolWorksheet.Cell(row, 9).Value = item.License;
            eolWorksheet.Cell(row, 10).Value = item.LicenseUrl;
            eolWorksheet.Cell(row, 11).Value = item.RecommendedAction;
            row++;
        }

        eolWorksheet.ColumnsUsed().AdjustToContents();
        var dataRange = eolWorksheet.Range(1, 1, Math.Max(1, row - 1), 11);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        eolWorksheet.Range(1, 1, 1, 11).SetAutoFilter();
    }
}
