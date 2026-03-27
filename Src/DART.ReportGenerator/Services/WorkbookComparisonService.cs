using ClosedXML.Excel;

namespace DART.ReportGenerator;

public sealed class WorkbookComparisonService
{
    public void ApplyComparison(IXLWorksheet currentWorksheet, IXLWorksheet previousWorksheet, int startRow = 8)
    {
        const int endColumn = 12;

        var maxRow1 = currentWorksheet.LastRowUsed()?.RowNumber() ?? startRow - 1;
        var maxRow2 = previousWorksheet.LastRowUsed()?.RowNumber() ?? startRow - 1;
        var previousRowsByKey = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var row2 = startRow; row2 <= maxRow2; row2++)
        {
            var key = BuildMatchKey(previousWorksheet, row2);
            previousRowsByKey.TryAdd(key, row2);
        }

        for (var row1 = startRow; row1 <= maxRow1; row1++)
        {
            var currentRowKey = BuildMatchKey(currentWorksheet, row1);
            var matchingRow2 = previousRowsByKey.TryGetValue(currentRowKey, out var matchedRow)
                ? matchedRow
                : -1;

            currentWorksheet.Cell(row1, 7).Value = matchingRow2 != -1 ? "Yes" : "No";

            if (matchingRow2 == -1)
            {
                continue;
            }

            for (var col = 9; col <= 12; col++)
            {
                currentWorksheet.Cell(row1, col).Value = previousWorksheet.Cell(matchingRow2, col).Value;
            }
        }

        if (maxRow1 >= startRow)
        {
            var dataRange = currentWorksheet.Range(startRow, 1, maxRow1, endColumn);
            var existingRule = dataRange.AddConditionalFormat().WhenIsTrue($"=$G{startRow}=\"Yes\"");
            existingRule.Fill.SetBackgroundColor(XLColor.LightBlue);

            var newRule = dataRange.AddConditionalFormat().WhenIsTrue($"=$G{startRow}=\"No\"");
            newRule.Fill.SetBackgroundColor(XLColor.LightPink);
        }

        currentWorksheet.Columns().AdjustToContents();
    }

    private static string BuildMatchKey(IXLWorksheet worksheet, int row)
    {
        var cell1 = worksheet.Cell(row, 1).Value.ToString();
        var cell2 = worksheet.Cell(row, 2).Value.ToString();
        var cell5 = worksheet.Cell(row, 5).Value.ToString();
        return string.Concat(cell1, "\u001F", cell2, "\u001F", cell5);
    }
}
