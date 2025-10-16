namespace DART.BlackduckAnalysis
{
    public class ExtractResult(string resultPath)
    {
        public string DestinationPath { get; set; } = resultPath;
    }
}