using DART.EOLAnalysis.Helpers;

namespace DART.EOLAnalysis.Models
{
    public class Repository
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Branch { get; set; } = "main";

        // These will be populated after parsing the URL
        public string Organization { get; private set; }
        public string Project { get; private set; }
        public string RepositoryName { get; private set; }

        public void ParseUrl()
        {
            var parsedInfo = ParseRepoUrlHelper.ParseRepoUrl(Url);
            Organization = parsedInfo.Organization;
            Project = parsedInfo.Project;
            RepositoryName = parsedInfo.RepoName;
        }
    }
}