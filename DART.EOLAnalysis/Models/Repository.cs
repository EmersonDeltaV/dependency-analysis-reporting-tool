using DART.EOLAnalysis.Helpers;

namespace DART.EOLAnalysis.Models
{
    public class Repository
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Branch { get; set; } = "main";

        /// <summary>
        /// List of file paths to exclude from analysis (e.g., "src/legacy/", "tests/"). This allows users to skip irrelevant files or directories that may contain outdated dependencies but are not critical to the analysis.
        /// </summary>
        public List<string> FileSkipFilter { get; set; } = [];

        // These will be populated after parsing the URL
        public string Organization { get; private set; } = string.Empty;
        public string Project { get; private set; } = string.Empty;
        public string RepositoryName { get; private set; } = string.Empty;

        public void ParseUrl()
        {
            var parsedInfo = ParseRepoUrlHelper.ParseRepoUrl(Url);
            Organization = parsedInfo.Organization;
            Project = parsedInfo.Project;
            RepositoryName = parsedInfo.RepoName;
        }
    }
}