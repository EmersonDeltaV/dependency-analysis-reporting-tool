using ClosedXML.Excel;

namespace DART.ReportGenerator.Services;

public sealed class WorkbookComparisonService
{
    public void ApplyComparison(IXLWorksheet currentWorksheet, IXLWorksheet previousWorksheet, int startRow = 8)
    {
        const int endMatchColumn = 5;
        const int endColumn = 12;

        var maxRow1 = currentWorksheet.LastRowUsed()?.RowNumber() ?? startRow - 1;
        var maxRow2 = previousWorksheet.LastRowUsed()?.RowNumber() ?? startRow - 1;

        for (var row1 = startRow; row1 <= maxRow1; row1++)
        {
            var matchingRow2 = -1;

            for (var row2 = startRow; row2 <= maxRow2; row2++)
            {
                var allColumnsMatch = true;

                for (var col = 1; col <= endMatchColumn; col++)
                {
                    if (col == 3 || col == 4)
                    {
                        continue;
                    }

                    var cell1 = currentWorksheet.Cell(row1, col).Value.ToString();
                    var cell2 = previousWorksheet.Cell(row2, col).Value.ToString();

                    if (!string.Equals(cell1, cell2, StringComparison.Ordinal))
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
}
