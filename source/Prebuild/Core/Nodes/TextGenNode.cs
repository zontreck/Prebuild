﻿using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Prebuild.Core.Nodes
{
    [DataNode("TextGen")]
    public class TextGenNode : DataNode
    {
        #region Values
        private string m_Name;
        private string m_OutputName;
        private List<ReferenceNode> m_Libs = new();
        private string m_Tool;
        

        #endregion

        #region Methods
        public override void Parse(XmlNode node)
        {
            m_Name = Helper.AttributeValue(node, "name", "");
            m_OutputName = Helper.AttributeValue(node, "output", "");
            m_Tool = Helper.AttributeValue(node, "tool", "SnapWrap");

            foreach (XmlNode childNode in node.ChildNodes)
            {
                var data = Kernel.Instance.ParseNode(childNode, this);
                if(data is ReferenceNode rn)
                {
                    m_Libs.Add(rn);
                }
            }

            SourceInSolution = Helper.ParseBoolean(node, "sourceInSolution", false);


            if (m_Tool == "Bottle")
            {
                // Add to the extension: Bottle.cs
                // This is to aid in excluding these files from git
                string desiredExtension = Path.GetExtension(OutputName);
                m_OutputName = Path.ChangeExtension(m_OutputName, ".Bottle" + desiredExtension);


            }
            else
            {
                string desiredExtension = Path.GetExtension(OutputName);
                m_OutputName = Path.ChangeExtension(m_OutputName, ".SnapWrap" + desiredExtension);
            }

        }

        public override void Write(XmlDocument doc, XmlElement current)
        {
            XmlElement cur = doc.CreateElement("TextGen");
            cur.SetAttribute("name", m_Name);
            cur.SetAttribute("output", m_OutputName);
            cur.SetAttribute("tool", m_Tool);

            foreach(var reference in m_Libs)
            {
                reference.Write(doc, cur);
            }
            cur.SetAttribute("sourceInSolution", SourceInSolution ? bool.TrueString : bool.FalseString);

            current.AppendChild(cur);
        }
        #endregion

        #region Fields
        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public string OutputName
        {
            get
            {
                return m_OutputName;
            }
        }

        public string Libraries
        {
            get
            {
                List<string> tmp = new();
                foreach(var rn in m_Libs)
                {
                    tmp.Add(rn.Name);
                }

                return String.Join("..", tmp);
            }
        }

        public bool SourceInSolution { get; internal set; } = false;

        public string SourceDirectory
        {
            get
            {
                return SourceInSolution ? "SolutionDir" : "ProjectDir";
            }
        }

        public string Tool
        {
            get
            {
                return m_Tool;
            }
        }

        #endregion
    }
}
