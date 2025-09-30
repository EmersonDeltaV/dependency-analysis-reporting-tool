namespace DART.EOLAnalysis.Models
{
    public class GitItem
    {
        public string ObjectId { get; set; } = string.Empty;
        public string GitObjectType { get; set; }  = string.Empty;
        public string CommitId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}