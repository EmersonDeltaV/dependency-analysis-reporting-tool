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
}
