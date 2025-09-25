using System.Xml.Linq;

namespace EOLAnalysisLib
{
    internal static class PackageConfigService
    {
        // Existing method - unchanged
        internal static IEnumerable<XElement>? GetPackages(string path)
        {
            XDocument doc = XDocument.Load(path);
            return ProcessXDocument(doc);
        }

        // New method for processing content from API
        internal static IEnumerable<XElement>? GetPackagesFromContent(string content)
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