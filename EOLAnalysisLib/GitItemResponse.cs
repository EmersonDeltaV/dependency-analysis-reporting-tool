namespace EOLAnalysisLib
{
    // Response models
    public class GitItemsResponse // Changed from private to public
    {
        public int Count { get; set; }
        public List<GitItem> Value { get; set; } = new List<GitItem>();
    }

    public class GitItem // Changed from private to public
    {
        public string ObjectId { get; set; }
        public string GitObjectType { get; set; }  // Changed from IsFolder to match API
        public string CommitId { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }
        public string Url { get; set; }  // Added this as it's in the response
    }
}