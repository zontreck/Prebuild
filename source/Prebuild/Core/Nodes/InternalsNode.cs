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
    [DataNode("InternalsVisibleTo")]
    public class InternalsNode : DataNode
    {
        public string Name { get; internal set; }

        #region Methods
        public override void Parse(XmlNode node)
        {
            base.Parse(node);
            Name = Helper.AttributeValue(node, "name", "");
        }

        public override void Write(XmlDocument doc, XmlElement current)
        {
            XmlElement main = doc.CreateElement("InternalsVisibleTo");
            main.SetAttribute("name", Name);

            current.AppendChild(main);
        }
        #endregion
    }
}
