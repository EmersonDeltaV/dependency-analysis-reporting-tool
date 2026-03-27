using ClosedXML.Excel;
using DART.ReportGenerator;

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

    [Fact]
    public void ApplyComparison_ShouldUseFirstPreviousMatch_WhenDuplicateKeysExist()
    {
        using var currentWorkbook = new XLWorkbook();
        using var previousWorkbook = new XLWorkbook();

        var currentSheet = currentWorkbook.AddWorksheet("Black Duck Security Risks");
        var previousSheet = previousWorkbook.AddWorksheet("Black Duck Security Risks");

        currentSheet.Cell(8, 1).Value = "APP01";
        currentSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        currentSheet.Cell(8, 3).Value = "Current risk";
        currentSheet.Cell(8, 4).Value = "Current severity";
        currentSheet.Cell(8, 5).Value = "CVE-2024-10001";

        previousSheet.Cell(8, 1).Value = "APP01";
        previousSheet.Cell(8, 2).Value = "Newtonsoft.Json";
        previousSheet.Cell(8, 3).Value = "Older risk";
        previousSheet.Cell(8, 4).Value = "Older severity";
        previousSheet.Cell(8, 5).Value = "CVE-2024-10001";
        previousSheet.Cell(8, 9).Value = "First row status";
        previousSheet.Cell(8, 10).Value = "First row notes";
        previousSheet.Cell(8, 11).Value = "First row state";
        previousSheet.Cell(8, 12).Value = "First row owner";

        previousSheet.Cell(9, 1).Value = "APP01";
        previousSheet.Cell(9, 2).Value = "Newtonsoft.Json";
        previousSheet.Cell(9, 3).Value = "Newer risk";
        previousSheet.Cell(9, 4).Value = "Newer severity";
        previousSheet.Cell(9, 5).Value = "CVE-2024-10001";
        previousSheet.Cell(9, 9).Value = "Second row status";
        previousSheet.Cell(9, 10).Value = "Second row notes";
        previousSheet.Cell(9, 11).Value = "Second row state";
        previousSheet.Cell(9, 12).Value = "Second row owner";

        var sut = new WorkbookComparisonService();

        sut.ApplyComparison(currentSheet, previousSheet, startRow: 8);

        Assert.Equal("Yes", currentSheet.Cell(8, 7).GetString());
        Assert.Equal("First row status", currentSheet.Cell(8, 9).GetString());
        Assert.Equal("First row notes", currentSheet.Cell(8, 10).GetString());
        Assert.Equal("First row state", currentSheet.Cell(8, 11).GetString());
        Assert.Equal("First row owner", currentSheet.Cell(8, 12).GetString());
    }
}
