using ClosedXML.Excel;
using DART.Tests.DART.ReportGenerator.TestSupport;

namespace DART.Tests.DART.ReportGenerator;

public class CurrentFormatWorkbookCharacterizationTests
{
    [Fact]
    public void BuildWorkbook_ShouldMatchCurrentHeaderLayout()
    {
        var generatorType = GetRequiredType("DART.ReportGenerator.ReportGenerator");
        var sut = Activator.CreateInstance(generatorType!);

        var buildWorkbookMethod = generatorType!.GetMethod("BuildWorkbook");
        Assert.NotNull(buildWorkbookMethod);
        if (buildWorkbookMethod is null)
        {
            return;
        }

        var workbookObject = buildWorkbookMethod.Invoke(
            sut,
            [ReportFixtureFactory.CreateSampleRows(), "Test Product", "1.0.0", "Sprint 25"]);

        var workbook = Assert.IsType<XLWorkbook>(workbookObject);
        using (workbook)
        {
            var worksheet = workbook.Worksheet("Black Duck Security Risks");

            Assert.Equal("Test Product", worksheet.Cell(1, 1).GetString());
            Assert.Equal("1.0.0", worksheet.Cell(2, 1).GetString());
            Assert.Equal("Sprint 25", worksheet.Cell(3, 1).GetString());

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
    }

    [Fact]
    public void BuildOutputFileName_ShouldMatchCurrentNamingPattern()
    {
        var generatorType = GetRequiredType("DART.ReportGenerator.ReportGenerator");
        var sut = Activator.CreateInstance(generatorType!);

        var buildOutputFileNameMethod = generatorType!.GetMethod("BuildOutputFileName");
        Assert.NotNull(buildOutputFileNameMethod);
        if (buildOutputFileNameMethod is null)
        {
            return;
        }

        var fileNameObject = buildOutputFileNameMethod.Invoke(
            sut,
            ["APP01", new DateTime(2026, 3, 25, 10, 11, 12)]);

        var fileName = Assert.IsType<string>(fileNameObject);

        Assert.Matches(
            "^dart-summary-APP01-\\d{4}-\\d{2}-\\d{2}-\\d{6}\\.xlsx$",
            fileName);
    }

    [Fact]
    public void ApplyComparison_ShouldCarryOverReviewColumns_AndMarkFoundInPreviousScan()
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

        var comparisonServiceType = GetRequiredType("DART.ReportGenerator.WorkbookComparisonService");
        var sut = Activator.CreateInstance(comparisonServiceType!);

        var applyComparisonMethod = comparisonServiceType!.GetMethod("ApplyComparison");
        Assert.NotNull(applyComparisonMethod);
        if (applyComparisonMethod is null)
        {
            return;
        }

        applyComparisonMethod.Invoke(sut, [currentSheet, previousSheet, 8]);

        Assert.Equal("Yes", currentSheet.Cell(8, 7).GetString());
        Assert.Equal("Yes", currentSheet.Cell(8, 9).GetString());
        Assert.Equal("Planned remediation", currentSheet.Cell(8, 10).GetString());
        Assert.Equal("In Progress", currentSheet.Cell(8, 11).GetString());
        Assert.Equal("Owner: Security", currentSheet.Cell(8, 12).GetString());
    }

    private static Type? GetRequiredType(string fullTypeName)
    {
        var resolvedType = Type.GetType($"{fullTypeName}, DART.ReportGenerator", throwOnError: false);
        Assert.NotNull(resolvedType);
        return resolvedType;
    }
}

