public static class ParseRepoUrlHelper
{
    public static (string Organization, string Project, string RepoName) ParseRepoUrl(string repoUrl)
    {
        // Parse URL like https://dev.azure.com/{org}/{project}/_git/{repo}
        Uri uri = new Uri(repoUrl);
        string path = uri.AbsolutePath;

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Standard format has organization, project and repo name
        if (segments.Length >= 3 && segments[segments.Length - 2] == "_git")
        {
            string organization = segments[0];
            // Project name might contain slashes, so we need to combine all segments between org and _git
            string project = string.Join("/", segments.Skip(1).Take(segments.Length - 3));
            string repoName = segments[segments.Length - 1];

            return (organization, project, repoName);
        }

        throw new ArgumentException("Invalid repository URL format", nameof(repoUrl));
    }
}