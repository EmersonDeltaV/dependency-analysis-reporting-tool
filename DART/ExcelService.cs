using BlackduckReportAnalysis.Models;
using BlackduckReportGeneratorTool;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BlackduckReportAnalysis
{
    public class ExcelService : IExcelService
    {
        private readonly ILogger<BlackduckReportAnalysisProgram> _logger;
        private readonly Config _config;

        private int currentRow = 8;
        private XLWorkbook xLWorkbook;
        private IXLWorksheet worksheet;

        public ExcelService(IConfiguration configuration,
                            ILogger<BlackduckReportAnalysisProgram> logger)
        {
            _config = configuration.Get<Config>() ?? throw new ConfigException("Failed to load configuration");
            _logger = logger;

            Initialize();
        }

        /// <summary>
        /// Initializes the Excel workbook and worksheet for Black Duck Security Risks summary report.
        /// </summary>
        private void Initialize()
        {
            xLWorkbook = new XLWorkbook();
            worksheet = xLWorkbook.Worksheets.Add("Black Duck Security Risks");
            FormatHeader(worksheet);
        }

        private void FormatHeader(IXLWorksheet worksheet)
        {
            //format general details detail
            worksheet.Range(1, 1, 1, 11).Merge();
            worksheet.Range(2, 1, 2, 11).Merge();
            worksheet.Range(3, 1, 3, 11).Merge();

            worksheet.Cell(1, 1).Value = _config.ProductName;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = _config.ProductVersion;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(3, 1).Value = _config.ProductIteration;
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
            worksheet.Range("A7:K7").SetAutoFilter();

            // Extract headers details and populate
            Type headersType = typeof(Models.Headers);
            FieldInfo[] fields = headersType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldValue = fields[i].GetValue(null)?.ToString() ?? string.Empty;
                worksheet.Cell(7, i + 1).Value = fieldValue;
            }

            //format cells, add colors and borders
            for (int i = 1; i <= 7; i++)
            {
                var cell = worksheet.Cell(7, i);
                cell.Style.Fill.BackgroundColor = XLColor.Green;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            for (int i = 8; i <= 11; i++)
            {
                var cell = worksheet.Cell(7, i);
                cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        /// <summary>
        /// Populates a row in the Black Duck Security Risks summary report with the provided row details.
        /// </summary>
        /// <param name="rowDetails">The details of the row to be populated.</param>
        public void PopulateRow(RowDetails rowDetails)
        {
            worksheet.Cell(currentRow, 1).Value = rowDetails.ApplicationName;
            worksheet.Cell(currentRow, 2).Value = rowDetails.SoftwareComponent;
            worksheet.Cell(currentRow, 3).Value = rowDetails.Version;
            worksheet.Cell(currentRow, 4).Value = rowDetails.SecurityRisk;
            worksheet.Cell(currentRow, 5).Value = rowDetails.VulnerabilityId;
            worksheet.Cell(currentRow, 6).Value = rowDetails.RecommendedFix;
            worksheet.Cell(currentRow, 8).Value = rowDetails.MatchType;

            if (string.IsNullOrEmpty(rowDetails.RecommendedFix))
            {
                _logger.LogWarning($"Row [{currentRow}] No recommended fix found for {rowDetails.ApplicationName} | {rowDetails.SoftwareComponent} | {rowDetails.VulnerabilityId}");
            }
            else
            {
                _logger.LogInformation($"Row [{currentRow}] {rowDetails.ApplicationName} | {rowDetails.SoftwareComponent} | {rowDetails.VulnerabilityId}");
            }

            currentRow++;
        }

        /// <summary>
        /// Saves the Black Duck Security Risks summary report.
        /// </summary>
        public void SaveReport()
        {
            worksheet.Columns().AdjustToContents();
            xLWorkbook.SaveAs(Path.Combine(_config.OutputFilePath, $"blackduck-summary-{DateTime.Now:yyyy-MM-dd-HHmmss}.xlsx"));
            _logger.LogInformation("Blackduck Analysis is completed and report was generated successfully.");
            xLWorkbook.Dispose();
        }

        public void CompareExcelFiles(string filePath1, string filePath2, string outputFilePath)
        {
            using (var workbook1 = new XLWorkbook(filePath1))
            using (var workbook2 = new XLWorkbook(filePath2))
            using (var outputWorkbook = new XLWorkbook())
            {
                var worksheet1 = workbook1.Worksheet(1);
                var worksheet2 = workbook2.Worksheet(1);
                var outputWorksheet = outputWorkbook.Worksheets.Add("Comparison");

                int startRow = 8;
                int endMatchColumn = 5; // Column E
                int endColumn = 12; // Column L
                var maxRow1 = worksheet1.LastRowUsed().RowNumber();
                var maxRow2 = worksheet2.LastRowUsed().RowNumber();

                for (int row1 = startRow; row1 <= maxRow1; row1++)
                {
                    int matchingRow2 = -1;

                    for (int row2 = startRow; row2 <= maxRow2; row2++)
                    {
                        bool allColumnsMatch = true;

                        for (int col = 1; col <= endMatchColumn; col++)
                        {
                            if (col == 3 || col == 4) // Skip column C and D
                                continue;

                            var cell1 = worksheet1.Cell(row1, col);
                            var cell2 = worksheet2.Cell(row2, col);

                            if (cell1.Value.ToString() != cell2.Value.ToString())
                            {
                                allColumnsMatch = false;
                                break;
                            }
                        }

                        if (allColumnsMatch)
                        {
                            matchingRow2 = row2;
                            break;
                        }
                    }

                    for (int col = 1; col <= endColumn; col++)
                    {
                        var cell1 = worksheet1.Cell(row1, col);
                        var outputCell = outputWorksheet.Cell(row1, col);
                        outputCell.Value = cell1.Value;

                        if (matchingRow2 != -1)
                        {
                            outputCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        }
                        else
                        {
                            outputCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        }
                    }

                    if (matchingRow2 != -1)
                    {
                        for (int col = 9; col <= 12; col++) // Columns I to L
                        {
                            var cell2 = worksheet2.Cell(matchingRow2, col);
                            var outputCell = outputWorksheet.Cell(row1, col);
                            outputCell.Value = cell2.Value;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Found 1 new finding.");
                    }
                }

                FormatHeader(outputWorksheet);
                outputWorksheet.Columns().AdjustToContents();
                outputWorkbook.SaveAs(Path.Combine(_config.OutputFilePath, $"blackduck-diff-{DateTime.Now:yyyy-MM-dd-HHmmss}.xlsx"));
            }
        }
    }
}
