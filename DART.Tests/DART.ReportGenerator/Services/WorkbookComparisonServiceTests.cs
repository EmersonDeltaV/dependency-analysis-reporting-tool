using ClosedXML.Excel;
using DART.ReportGenerator.Services;

namespace DART.Tests.DART.ReportGenerator.Services;

public class WorkbookComparisonServiceTests
{
    [Fact]
    public void ApplyComparison_ShouldCarryOverReviewColumns_AndSetFoundInPreviousScanFlag()
    {
        using var currentWorkbook = new XLWorkbook();
        using var previousWorkbook = new XLWorkbook();

        var currentSheet = currentWorkbook.AddWorksheet("Black Duck Security Risks");
        var previousSheet = previousWorkbook.AddWorksheet("Black Duck Security Risks");

        currentSheet.Cell(8, 1).Value = "APP01";
        currentSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        currentSheet.Cell(8, 5).Value = "CVE-2024-10001";

        previousSheet.Cell(8, 1).Value = "APP01";
        previousSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        previousSheet.Cell(8, 5).Value = "CVE-2024-10001";
        previousSheet.Cell(8, 9).Value = "Yes";
        previousSheet.Cell(8, 10).Value = "Planned remediation";
        previousSheet.Cell(8, 11).Value = "In Progress";
        previousSheet.Cell(8, 12).Value = "Owner: Security";

        var sut = new WorkbookComparisonService();

        sut.ApplyComparison(currentSheet, previousSheet, startRow: 8);

        Assert.Equal("Yes", currentSheet.Cell(8, 7).GetString());
        Assert.Equal("Yes", currentSheet.Cell(8, 9).GetString());
        Assert.Equal("Planned remediation", currentSheet.Cell(8, 10).GetString());
        Assert.Equal("In Progress", currentSheet.Cell(8, 11).GetString());
        Assert.Equal("Owner: Security", currentSheet.Cell(8, 12).GetString());
    }

    [Fact]
    public void ApplyComparison_ShouldAddConditionalFormattingRules_ForExistingAndNewFindings()
    {
        using var currentWorkbook = new XLWorkbook();
        using var previousWorkbook = new XLWorkbook();

        var currentSheet = currentWorkbook.AddWorksheet("Black Duck Security Risks");
        var previousSheet = previousWorkbook.AddWorksheet("Black Duck Security Risks");

        currentSheet.Cell(8, 1).Value = "APP01";
        currentSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        currentSheet.Cell(8, 5).Value = "CVE-2024-10001";

        previousSheet.Cell(8, 1).Value = "APP01";
        previousSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        previousSheet.Cell(8, 5).Value = "CVE-2024-10001";

        var sut = new WorkbookComparisonService();

        sut.ApplyComparison(currentSheet, previousSheet, startRow: 8);

        var cfCount = currentSheet.ConditionalFormats.Count();
        Assert.True(cfCount >= 2);
    }
}
