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
using System.IO;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
public enum BuildAction
{
    /// <summary>
    /// </summary>
    None,

    /// <summary>
    /// </summary>
    Compile,

    /// <summary>
    /// </summary>
    Content,

    /// <summary>
    /// </summary>
    EmbeddedResource,

    /// <summary>
    /// </summary>
    ApplicationDefinition,

    /// <summary>
    /// </summary>
    Page,

    /// <summary>
    /// </summary>
    Copy
}

/// <summary>
/// </summary>
public enum SubType
{
    /// <summary>
    /// </summary>
    Code,

    /// <summary>
    /// </summary>
    Component,

    /// <summary>
    /// </summary>
    Designer,

    /// <summary>
    /// </summary>
    Form,

    /// <summary>
    /// </summary>
    Settings,

    /// <summary>
    /// </summary>
    UserControl,

    /// <summary>
    /// </summary>
    CodeBehind
}

public enum CopyToOutput
{
    Never,
    Always,
    PreserveNewest
}

/// <summary>
/// </summary>
[DataNode("File")]
public class FileNode : DataNode
{
    #region Public Methods

    /// <summary>
    /// </summary>
    /// <param name="node"></param>
    public override void Parse(XmlNode node)
    {
        var buildAction = Helper.AttributeValue(node, "buildAction", string.Empty);
        if (buildAction != string.Empty)
            m_BuildAction = (BuildAction)Enum.Parse(typeof(BuildAction), buildAction);
        var subType = Helper.AttributeValue(node, "subType", string.Empty);
        if (subType != string.Empty)
            m_SubType = (SubType)Enum.Parse(typeof(SubType), subType);

        Console.WriteLine("[FileNode]:BuildAction is {0}", buildAction);


        ResourceName = Helper.AttributeValue(node, "resourceName", ResourceName);
        IsLink = bool.Parse(Helper.AttributeValue(node, "link", bool.FalseString));
        if (IsLink) LinkPath = Helper.AttributeValue(node, "linkPath", string.Empty);
        CopyToOutput = (CopyToOutput)Enum.Parse(typeof(CopyToOutput),
            Helper.AttributeValue(node, "copyToOutput", CopyToOutput.ToString()));
        PreservePath = bool.Parse(Helper.AttributeValue(node, "preservePath", bool.FalseString));

        if (node == null) throw new ArgumentNullException("node");

        Path = Helper.InterpolateForEnvironmentVariables(node.InnerText);
        if (Path == null) Path = "";

        Path = Path.Trim();
        IsValid = true;
        if (!File.Exists(Path))
        {
            IsValid = false;
            Kernel.Instance.Log.Write(LogType.Warning, "File does not exist: {0}", Path);
        }

        if (System.IO.Path.GetExtension(Path) == ".settings")
        {
            m_SubType = SubType.Settings;
            m_BuildAction = BuildAction.None;
        }
    }

    public override void Write(XmlDocument doc, XmlElement current)
    {
        XmlElement main = doc.CreateElement("File");
        main.SetAttribute("buildAction", BuildAction.ToString());
        main.SetAttribute("subType", SubType.ToString());
        main.SetAttribute("resourceName", ResourceName);
        main.SetAttribute("link", IsLink ? bool.TrueString : bool.FalseString);
        main.SetAttribute("linkPath", LinkPath);
        main.SetAttribute("copyToOutput", CopyToOutput.ToString());
        main.SetAttribute("preservePath", PreservePath ? bool.TrueString : bool.FalseString);
        main.InnerText = Path;

        current.AppendChild(main);
    }

    #endregion

    #region Fields

    private BuildAction? m_BuildAction;
    private SubType? m_SubType;

    #endregion

    #region Properties

    /// <summary>
    /// </summary>
    public string Path { get; internal set; }

    /// <summary>
    /// </summary>
    public string ResourceName { get; internal set; } = "";

    /// <summary>
    /// </summary>
    public BuildAction BuildAction
    {
        get
        {
            if (m_BuildAction != null)
                return m_BuildAction.Value;
            return GetBuildActionByFileName(Path);
        }
    }

    public CopyToOutput CopyToOutput { get; internal set; } = CopyToOutput.Never;

    public bool IsLink { get; internal set; }

    public string LinkPath { get; internal set; } = string.Empty;

    /// <summary>
    /// </summary>
    public SubType SubType
    {
        get
        {
            if (m_SubType != null)
                return m_SubType.Value;
            return GetSubTypeByFileName(Path);
        }
    }

    /// <summary>
    /// </summary>
    public bool IsValid { get; internal set; }

    /// <summary>
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public bool PreservePath { get; internal set; }

    #endregion
}