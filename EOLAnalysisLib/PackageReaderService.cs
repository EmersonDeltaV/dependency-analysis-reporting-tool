using Microsoft.Build.Construction;

namespace EOLAnalysisLib
{
    internal static class PackageReaderService
    {
        public static IEnumerable<ProjectItemElement> ReadProjectPackages(string csProjPath)
        {
            // Load the project


            var project = ProjectRootElement.Open(csProjPath);

            // Get the ItemGroup that contains package references
            var packageReferenceGroup = project.ItemGroups
                .Where(group => group.Items.Any(item => item.ItemType == "PackageReference"))
                .FirstOrDefault();

            if (packageReferenceGroup != null)
            {
                // Get all package references
                var packageReferences = packageReferenceGroup.Items.Where(item => item.ItemType == "PackageReference");

                return packageReferences;

                //// Print each package's ID and version
                //foreach (var packageReference in packageReferences)
                //{
                //    Console.WriteLine($"ID: {packageReference.Include}");
                //    Console.WriteLine($"Version: {packageReference.Metadata.FirstOrDefault(m => m.Name == "Version")?.Value}");
                //    Console.WriteLine();
                //}
            }

            throw new NullReferenceException();
        }
    }
}