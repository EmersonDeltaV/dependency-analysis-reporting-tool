using System.Xml.Linq;

namespace DART.EOLAnalysis.Helpers
{
    public static class PackageConfigHelper
    {
        // Parse package references from .csproj content
        public static IEnumerable<XElement>? GetPackagesFromContent(string content)
        {
            XDocument doc = XDocument.Parse(content);
            return ProcessXDocument(doc);
        }

        // Common processing logic
        private static IEnumerable<XElement>? ProcessXDocument(XDocument doc)
        {
            if (doc.Root == null)
            {
                throw new NullReferenceException("The XML document does not have a root element.");
            }

            // Get all package references
            var packageReferences = doc.Root.Descendants("PackageReference");

            if (packageReferences.Any())
            {
                return packageReferences;
            }

            Console.WriteLine("No package references found in the XML document.");
            return null;
        }
    }

}