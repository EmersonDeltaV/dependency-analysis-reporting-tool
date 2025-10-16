namespace DART.BlackduckAnalysis
{
    public interface IBlackduckReportGenerator
    {
        Task GenerateReport();

        Task Cleanup();
    }
}
