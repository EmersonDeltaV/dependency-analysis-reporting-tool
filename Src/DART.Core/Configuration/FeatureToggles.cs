namespace DART.Core;

public class FeatureToggles
{
    /// <summary>
    /// When true, runs all Black Duck download, processing, and comparison steps. When false, Black Duck steps are skipped. Defaults to true. Black Duck configuration is only required when this is true.
    /// </summary>
    public bool EnableBlackduckAnalysis { get; set; } = true;

    /// <summary>
    /// When true, adds an EOL analysis sheet for CSharp projects. Can run standalone (with Black Duck disabled) or alongside Black Duck. Requires EOL repo configuration.
    /// </summary>
    public bool EnableCSharpAnalysis { get; set; } = true;

    /// <summary>
    /// When true, adds an EOL analysis sheet for NPM projects. Can run standalone (with Black Duck disabled) or alongside Black Duck. Requires EOL repo configuration.
    /// </summary>
    public bool EnableNpmAnalysis { get; set; } = true;

    /// <summary>
    /// When true, includes dev dependencies in NPM EOL analysis. Defaults to false.
    /// </summary>
    public bool IncludeNpmDevDependencies { get; set; } = false;

}
