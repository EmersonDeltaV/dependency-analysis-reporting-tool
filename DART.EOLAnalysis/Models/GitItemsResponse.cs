namespace DART.EOLAnalysis.Models
{
    public class GitItemsResponse
    {
        public int Count { get; set; }
        public List<GitItem> Value { get; set; } = new List<GitItem>();
    }
}