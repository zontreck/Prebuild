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
    [DataNode("TextGen")]
    public class TextGenNode : DataNode
    {
        #region Values
        private string m_Name;
        private string m_OutputName;
        private List<string> m_Libs = new();
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
                if(data is ReferenceNode)
                {
                    m_Libs.Add(((ReferenceNode)data).Name);
                }
            }

            SourceInSolution = Helper.ParseBoolean(node, "sourceInSolution", false);
            
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
                return String.Join("..", m_Libs);
            }
        }

        public bool SourceInSolution { get; private set; } = false;

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
