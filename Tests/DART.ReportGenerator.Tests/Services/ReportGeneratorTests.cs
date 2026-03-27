using ClosedXML.Excel;
using DART.ReportGenerator;
using DART.Tests.DART.ReportGenerator.TestSupport;
using ReportGeneratorService = DART.ReportGenerator.ReportGenerator;

namespace DART.Tests.DART.ReportGenerator.Services;

public class ReportGeneratorTests
{
    [Fact]
    public void BuildWorkbook_ShouldCreateBlackDuckSecurityRisksWorksheet_WithExpectedHeaders()
    {
        var sut = new ReportGeneratorService();

        using var workbook = sut.BuildWorkbook(
            ReportFixtureFactory.CreateSampleRows(),
            productName: "Test Product",
            productVersion: "1.0.0",
            productIteration: "Sprint 25");

        var worksheet = workbook.Worksheet("Black Duck Security Risks");

        Assert.Equal(ExpectedHeaders.Application, worksheet.Cell(7, 1).GetString());
        Assert.Equal(ExpectedHeaders.SoftwareComponent, worksheet.Cell(7, 2).GetString());
        Assert.Equal(ExpectedHeaders.Version, worksheet.Cell(7, 3).GetString());
        Assert.Equal(ExpectedHeaders.SecurityRisk, worksheet.Cell(7, 4).GetString());
        Assert.Equal(ExpectedHeaders.VulnerabilityId, worksheet.Cell(7, 5).GetString());
        Assert.Equal(ExpectedHeaders.RecommendedFixVersion, worksheet.Cell(7, 6).GetString());
        Assert.Equal(ExpectedHeaders.FoundInPreviousScan, worksheet.Cell(7, 7).GetString());
        Assert.Equal(ExpectedHeaders.MatchType, worksheet.Cell(7, 8).GetString());
        Assert.Equal(ExpectedHeaders.ReviewWithCS, worksheet.Cell(7, 9).GetString());
        Assert.Equal(ExpectedHeaders.ActionPlan, worksheet.Cell(7, 10).GetString());
        Assert.Equal(ExpectedHeaders.FinalStatus, worksheet.Cell(7, 11).GetString());
        Assert.Equal(ExpectedHeaders.Notes, worksheet.Cell(7, 12).GetString());
    }

    [Fact]
    public void BuildOutputFileName_ShouldFollowCurrentPattern()
    {
        var sut = new ReportGeneratorService();

        var fileName = sut.BuildOutputFileName("APP01", new DateTime(2026, 3, 25, 10, 11, 12));

        Assert.Matches("^dart-summary-APP01-\\d{4}-\\d{2}-\\d{2}-\\d{6}\\.xlsx$", fileName);
    }

    [Fact]
    public void BuildWorkbook_ShouldMergeTitleRowsAcrossAllHeaderColumns()
    {
        var sut = new ReportGeneratorService();

        using var workbook = sut.BuildWorkbook(
            ReportFixtureFactory.CreateSampleRows(),
            productName: "Test Product",
            productVersion: "1.0.0",
            productIteration: "Sprint 25");

        var worksheet = workbook.Worksheet("Black Duck Security Risks");

        Assert.Contains(worksheet.Range("A1:L1"), worksheet.MergedRanges);
        Assert.Contains(worksheet.Range("A2:L2"), worksheet.MergedRanges);
        Assert.Contains(worksheet.Range("A3:L3"), worksheet.MergedRanges);
    }

    [Fact]
    public void CompareCurrentWithPrevious_ShouldUseSecurityRisksWorksheet_WhenWorksheetOrderChanges()
    {
        var currentPath = Path.Combine(Path.GetTempPath(), $"current-{Guid.NewGuid():N}.xlsx");
        var previousPath = Path.Combine(Path.GetTempPath(), $"previous-{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var currentWorkbook = new XLWorkbook())
            {
                currentWorkbook.Worksheets.Add("Summary");
                var currentSecurityWorksheet = currentWorkbook.Worksheets.Add("Black Duck Security Risks");
                currentSecurityWorksheet.Cell(8, 1).Value = "APP01";
                currentSecurityWorksheet.Cell(8, 2).Value = "Newtonsoft.Json";
                currentSecurityWorksheet.Cell(8, 5).Value = "CVE-2024-10001";
                currentWorkbook.SaveAs(currentPath);
            }

            using (var previousWorkbook = new XLWorkbook())
            {
                previousWorkbook.Worksheets.Add("Overview");
                var previousSecurityWorksheet = previousWorkbook.Worksheets.Add("Black Duck Security Risks");
                previousSecurityWorksheet.Cell(8, 1).Value = "APP01";
                previousSecurityWorksheet.Cell(8, 2).Value = "Newtonsoft.Json";
                previousSecurityWorksheet.Cell(8, 5).Value = "CVE-2024-10001";
                previousWorkbook.SaveAs(previousPath);
            }

            var sut = new ReportGeneratorService();

            sut.CompareCurrentWithPrevious(currentPath, previousPath);

            using var comparedWorkbook = new XLWorkbook(currentPath);
            var comparedWorksheet = comparedWorkbook.Worksheet("Black Duck Security Risks");
            Assert.Equal("Yes", comparedWorksheet.Cell(8, 7).GetString());
        }
        finally
        {
            if (File.Exists(currentPath))
            {
                File.Delete(currentPath);
            }

            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }
        }
    }

    [Fact]
    public void CompareCurrentWithPrevious_ShouldThrowClearError_WhenSecurityRisksWorksheetIsMissing()
    {
        var currentPath = Path.Combine(Path.GetTempPath(), $"current-{Guid.NewGuid():N}.xlsx");
        var previousPath = Path.Combine(Path.GetTempPath(), $"previous-{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var currentWorkbook = new XLWorkbook())
            {
                currentWorkbook.Worksheets.Add("Summary");
                currentWorkbook.SaveAs(currentPath);
            }

            using (var previousWorkbook = new XLWorkbook())
            {
                previousWorkbook.Worksheets.Add("Black Duck Security Risks");
                previousWorkbook.SaveAs(previousPath);
            }

            var sut = new ReportGeneratorService();

            var exception = Assert.Throws<InvalidOperationException>(() => sut.CompareCurrentWithPrevious(currentPath, previousPath));

            Assert.Contains("Black Duck Security Risks", exception.Message);
            Assert.Contains("current report", exception.Message);
        }
        finally
        {
            if (File.Exists(currentPath))
            {
                File.Delete(currentPath);
            }

            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }
        }
    }
}
