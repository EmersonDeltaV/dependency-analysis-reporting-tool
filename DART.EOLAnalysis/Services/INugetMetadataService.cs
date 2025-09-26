using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    public interface INugetMetadataService
    {
        void Initialize(string nugetApiUrl);
        Task GetDataAsync(PackageData data, CancellationToken cancellationToken = default);
    }
}