namespace DART.BlackduckAnalysis
{
    public interface IBlackduckReportGenerator
    {
        void SetRuntimeConfig(BlackduckConfiguration config, string reportFolderPath);

        Task GenerateReport();

        Task Cleanup();
    }
}
