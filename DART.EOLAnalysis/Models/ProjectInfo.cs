namespace DART.EOLAnalysis.Models
{
    public enum ProjectType
    {
        CSharp,
        Npm
    }

    public class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public ProjectType ProjectType { get; set; } = ProjectType.CSharp;
    }
}
