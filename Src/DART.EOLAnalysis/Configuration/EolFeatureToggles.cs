namespace DART.EOLAnalysis
{
    public class EolFeatureToggles
    {
        public bool EnableCSharpAnalysis { get; set; } = true;
        public bool EnableNpmAnalysis { get; set; } = true;
        public bool IncludeNpmDevDependencies { get; set; } = false;
    }
}
