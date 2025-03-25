namespace BlackduckReportGeneratorTool.Models.ServiceResult
{
    public class ExtractResult(string resultPath)
    {
        public string DestinationPath { get; set; } = resultPath;
    }
}