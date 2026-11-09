using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml.Linq;

using static PCL.Core.UI.ToastNotification;

namespace PCL.Core.Test;

[TestClass]
public class ToastTest
{
    [TestMethod]
    public void ToastTemplatePreservesTextAndEscapesXmlMarkup()
    {
        const string title = "A <title> & \"quote\"";
        const string message = "正文 & <text>";
        var template = XDocument.Parse(BuildTemplate(message, title));
        Assert.AreEqual("ToastGeneric", template.Descendants("binding").Single().Attribute("template")?.Value);
        CollectionAssert.AreEqual(new[] { title, message }, template.Descendants("text").Select(node => node.Value).ToArray());
    }
}
