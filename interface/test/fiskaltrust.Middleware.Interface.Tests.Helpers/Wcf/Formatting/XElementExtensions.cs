#if WCF
using System.Linq;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf.Formatting
{
    public static class XElementExtensions
    {
        public static void TransformToBeJsonCompliant(this XElement element)
        {
            XNamespace jsonNs = "http://james.newtonking.com/projects/json";
            element.Add(new XAttribute(XNamespace.Xmlns + "json", "http://james.newtonking.com/projects/json"));

            foreach (var descendant in element.Descendants().ToList())
            {
                var attr = descendant.Attribute("type");
                if (attr?.Value == "array")
                {
                    var childElements = descendant.Elements().ToList();
                    foreach (var childElement in childElements)
                    {
                        var newElem = new XElement(descendant.Name, childElement.Attributes(), childElement.Elements());
                        newElem.Add(new XAttribute(jsonNs + "Array", "true"));

                        descendant.Parent.Add(newElem);
                        childElement.Remove();
                    }
                    if (childElements.Any())
                        descendant.Remove();
                }
            }
            foreach (var descendant in element.Descendants())
            {
                descendant.Attribute("type")?.Remove();
            }
        }
    }
}
#endif