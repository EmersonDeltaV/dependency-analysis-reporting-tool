namespace DART.EOLAnalysis
{
    public class GitItemsResponse
    {
        public int Count { get; set; }
        public List<GitItem> Value { get; set; } = new List<GitItem>();
    }
}