namespace DART.EOLAnalysis.Models
{
    public class GitItem
    {
        public string ObjectId { get; set; }
        public string GitObjectType { get; set; }  // Changed from IsFolder to match API
        public string CommitId { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }
        public string Url { get; set; }  // Added this as it's in the response
    }
}