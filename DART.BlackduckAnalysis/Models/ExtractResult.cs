namespace DART.BlackduckAnalysis.Models
{
    public class ExtractResult(string resultPath)
    {
        public string DestinationPath { get; set; } = resultPath;
    }
}