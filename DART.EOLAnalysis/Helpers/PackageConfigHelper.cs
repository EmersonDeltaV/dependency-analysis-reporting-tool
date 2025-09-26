using System.Xml.Linq;

namespace DART.EOLAnalysis.Helpers
{
    public static class PackageConfigHelper
    {
        // Parse package references from .csproj content
        public static IEnumerable<XElement> GetPackagesFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));
            }

            XDocument doc = XDocument.Parse(content);
            return ProcessXDocument(doc);
        }

        // Common processing logic
        private static IEnumerable<XElement> ProcessXDocument(XDocument doc)
        {
            if (doc.Root == null)
            {
                throw new ArgumentException("The XML document does not have a root element.");
            }

            // Get all package references
            var packageReferences = doc.Root.Descendants("PackageReference");

            if (packageReferences.Any())
            {
                return packageReferences;
            }

            // Return empty enumerable instead of null when no packages found
            return Enumerable.Empty<XElement>();
        }
    }

}