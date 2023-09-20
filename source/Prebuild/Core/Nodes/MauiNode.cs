using Prebuild.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Prebuild.Core.Nodes
{
    [DataNode("Maui")]
    public class MauiNode : DataNode
    {
        public MauiTitle applicationTitle { get; internal set; } = null;

        public override void Parse(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var dataNode = Kernel.Instance.ParseNode(child, this);
                if (dataNode != null)
                {
                    if (dataNode is MauiTitle title) applicationTitle = title;
                }
            }
        }

        public override void Write(XmlDocument doc, XmlElement current)
        {
            XmlElement maui = doc.CreateElement("Maui");
            applicationTitle.Write(doc, maui);

            current.AppendChild(maui);
        }

        public override string ToString()
        {
            string ret = "<UseMaui>true</UseMaui>\n";
            if(applicationTitle != null)
            {
                ret += applicationTitle.ToString() + "\n";
            }


            return ret;
        }
    }
}
