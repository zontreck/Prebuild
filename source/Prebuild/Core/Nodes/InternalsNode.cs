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
        public string Name { get; private set; }

        #region Methods
        public override void Parse(XmlNode node)
        {
            base.Parse(node);
            Name = Helper.AttributeValue(node, "name", "");
        }
        #endregion
    }
}
