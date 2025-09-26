using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    public interface INugetMetadataService
    {
        Task GetDataAsync(PackageData data);
    }
}