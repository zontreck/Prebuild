using Prebuild.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Prebuild.Core.Nodes
{
    [DataNode("Title")]
    public class MauiTitle : DataNode
    {
        public string value { get; internal set; } = string.Empty;
        public override void Parse(XmlNode node)
        {
            value = node.InnerText;
        }

        public override void Write(XmlDocument doc, XmlElement current)
        {
            XmlElement main = doc.CreateElement("Title");
            main.InnerText = value;
            current.AppendChild(main);
        }

        public override string ToString()
        {
            return $"<ApplicationTitle>{value}</ApplicationTitle>";
        }
    }
}
