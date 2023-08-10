#region BSD License

/*
Copyright (c) 2004-2005 Matthew Holmes (matthew@wildfiregames.com), Dan Moorehead (dan05a@gmail.com)

Redistribution and use in source and binary forms, with or without modification, are permitted
provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions 
  and the following disclaimer. 
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
  and the following disclaimer in the documentation and/or other materials provided with the 
  distribution. 
* The name of the author may not be used to endorse or promote products derived from this software 
  without specific prior written permission. 

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#endregion

using System;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
[DataNode("Configuration")]
public class ConfigurationNode : DataNode, ICloneable, IComparable
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigurationNode" /> class.
    /// </summary>
    public ConfigurationNode()
    {
        Options = new OptionsNode();
    }

    #endregion

    #region ICloneable Members

    /// <summary>
    ///     Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>
    ///     A new object that is a copy of this instance.
    /// </returns>
    public object Clone()
    {
        var ret = new ConfigurationNode();
        ret.Name = Name;
        ret.m_Platform = m_Platform;
        Options.CopyTo(ret.Options);
        return ret;
    }

    #endregion

    #region IComparable Members

    public int CompareTo(object obj)
    {
        var that = (ConfigurationNode)obj;
        return Name.CompareTo(that.Name);
    }

    #endregion

    #region Fields

    private string m_Platform = "AnyCPU";

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the parent.
    /// </summary>
    /// <value>The parent.</value>
    public override IDataNode Parent
    {
        get => base.Parent;
        set
        {
            base.Parent = value;
            if (base.Parent is SolutionNode)
            {
                var node = (SolutionNode)base.Parent;
                if (node != null && node.Options != null) node.Options.CopyTo(Options);
            }
        }
    }

    /// <summary>
    ///     Identifies the platform for this specific configuration.
    /// </summary>
    public string Platform
    {
        get => m_Platform;
        set
        {
            switch ((value + "").ToLower())
            {
                case "x86":
                case "x64":
                    m_Platform = value;
                    break;
                case "itanium":
                    m_Platform = "Itanium";
                    break;
                default:
                    m_Platform = "AnyCPU";
                    break;
            }
        }
    }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; private set; } = "unknown";

    /// <summary>
    ///     Gets the name and platform for the configuration.
    /// </summary>
    /// <value>The name and platform.</value>
    public string NameAndPlatform
    {
        get
        {
            var platform = m_Platform;
            if (platform == "AnyCPU")
                platform = "Any CPU";

            return string.Format("{0}|{1}", Name, platform);
        }
    }

    /// <summary>
    ///     Gets or sets the options.
    /// </summary>
    /// <value>The options.</value>
    public OptionsNode Options { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Parses the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public override void Parse(XmlNode node)
    {
        Name = Helper.AttributeValue(node, "name", Name);
        Platform = Helper.AttributeValue(node, "platform", m_Platform);

        if (node == null) throw new ArgumentNullException("node");
        foreach (XmlNode child in node.ChildNodes)
        {
            var dataNode = Kernel.Instance.ParseNode(child, this);
            if (dataNode is OptionsNode) ((OptionsNode)dataNode).CopyTo(Options);
        }
    }

    /// <summary>
    ///     Copies to.
    /// </summary>
    /// <param name="conf">The conf.</param>
    public void CopyTo(ConfigurationNode conf)
    {
        Options.CopyTo(conf.Options);
    }

    #endregion
}