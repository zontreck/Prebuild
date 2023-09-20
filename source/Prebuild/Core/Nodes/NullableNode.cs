using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Prebuild.Core.Nodes
{
    [DataNode("Nullable")]
    public class NullableNode : DataNode
    {
        public override void Parse(XmlNode node)
        {
            base.Parse(node);
        }

        public override void Write(XmlDocument doc, XmlElement current)
        {
            XmlElement nullable = doc.CreateElement("Nullable");
            current.AppendChild(nullable);
        }
    }
}
