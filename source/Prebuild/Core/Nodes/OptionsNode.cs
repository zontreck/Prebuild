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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
[DataNode("Options")]
public class OptionsNode : DataNode
{
    #region Constructors

    /// <summary>
    ///     Initializes the <see cref="OptionsNode" /> class.
    /// </summary>
    static OptionsNode()
    {
        var t = typeof(OptionsNode);

        foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var attrs = f.GetCustomAttributes(typeof(OptionNodeAttribute), false);
            if (attrs == null || attrs.Length < 1) continue;

            var ona = (OptionNodeAttribute)attrs[0];
            m_OptionFields[ona.NodeName] = f;
        }
    }

    #endregion

    #region Fields

    private static readonly Dictionary<string, FieldInfo> m_OptionFields = new();

    /// <summary>
    /// </summary>
    [field: OptionNode("CompilerDefines")]
    public string CompilerDefines { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("OptimizeCode")]
    public bool OptimizeCode { get; set; } = true;

    /// <summary>
    /// </summary>
    [field: OptionNode("CheckUnderflowOverflow")]
    public bool CheckUnderflowOverflow { get; set; } = true;

    /// <summary>
    /// </summary>
    [field: OptionNode("AllowUnsafe")]
    public bool AllowUnsafe { get; set; } = true;

    /// <summary>
    /// </summary>
    [field: OptionNode("PreBuildEvent")]
    public string PreBuildEvent { get; set; } = "";


    /// <summary>
    /// </summary>
    [field: OptionNode("PostBuildEvent")]
    public string PostBuildEvent { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("PreBuildEventArgs")]
    public string PreBuildEventArgs { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("PostBuildEventArgs")]
    public string PostBuildEventArgs { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("RunPostBuildEvent")]
    public string RunPostBuildEvent { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("RunScript")]
    public string RunScript { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("WarningLevel")]
    public int WarningLevel { get; set; } = 4;

    /// <summary>
    /// </summary>
    [field: OptionNode("WarningsAsErrors")]
    public bool WarningsAsErrors { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("SuppressWarnings")]
    public string SuppressWarnings { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("Prefer32Bit")]
    public bool Prefer32Bit { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("OutDir")]
    public string OutDir { get; set; } = "bin/";

    [field: OptionNode("EnableDefaultItems")]
    public bool EnableDefaultItems { get; set; } = true;

    [field: OptionNode("UseCommonOutputDirectory")]
    public bool UseCommonOutputDirectory { get; set; } = false;

    [field: OptionNode("AppendTargetFrameworkToOutputPath")]
    public bool AppendTargetFrameworkToOutputPath { get; set; } = true;

    [field: OptionNode("AppendRuntimeIdentifierToOutputPath")]
    public bool AppendRuntimeIdentifierToOutputPath { get; set; } = true;


    /// <summary>
    /// </summary>
    [field: OptionNode("OutputPath")]
    public string OutputPath { get; set; } = "bin/";

    /// <summary>
    /// </summary>
    [field: OptionNode("OutputType")]
    public string OutputType { get; set; } = "Exe";

    /// <summary>
    /// </summary>
    [field: OptionNode("RootNamespace")]
    public string RootNamespace { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("GenerateDocumentation")]
    public bool GenerateDocumentation { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("GenerateXmlDocFile")]
    public bool GenerateXmlDocFile { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("XmlDocFile")]
    public string XmlDocFile { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("KeyFile")]
    public string KeyFile { get; set; } = "";

    /// <summary>
    /// </summary>
    [field: OptionNode("DebugInformation")]
    public bool DebugInformation { get; set; } = true;

    /// <summary>
    /// </summary>
    [field: OptionNode("RegisterComInterop")]
    public bool RegisterComInterop { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("RemoveIntegerChecks")]
    public bool RemoveIntegerChecks { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("IncrementalBuild")]
    public bool IncrementalBuild { get; set; } = false;

    /// <summary>
    /// </summary>
    [field: OptionNode("BaseAddress")]
    public string BaseAddress { get; set; } = "285212672";

    /// <summary>
    /// </summary>
    [field: OptionNode("FileAlignment")]
    public int FileAlignment { get; set; } = 4096;

    /// <summary>
    /// </summary>
    [field: OptionNode("NoStdLib")]
    public bool NoStdLib { get; set; } = false;

    [field: OptionNode("UseDependencyFile")]
    public bool UseDepsFile { get; } = true;


    [field: OptionNode("SelfContained")]
    public bool SelfContained { get; } = true;

    [field: OptionNode("UseRuntimeIdentifier")]
    public bool UseRuntimeIdentifier { get; } = false;

    private readonly List<string> m_FieldsDefined = new();

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the <see cref="object" /> at the specified index.
    /// </summary>
    /// <value></value>
    public object this[string index]
    {
        get
        {
            if (!m_OptionFields.ContainsKey(index)) return null;

            var f = m_OptionFields[index];
            return f.GetValue(this);
        }
    }

    /// <summary>
    ///     Gets the <see cref="object" /> at the specified index.
    /// </summary>
    /// <value></value>
    public object this[string index, object defaultValue]
    {
        get
        {
            var valueObject = this[index];
            if (valueObject != null && valueObject is string && ((string)valueObject).Length == 0) return defaultValue;
            return valueObject;
        }
    }

    #endregion

    #region Private Methods

    private void FlagDefined(string name)
    {
        if (!m_FieldsDefined.Contains(name)) m_FieldsDefined.Add(name);
    }

    private void SetOption(string nodeName, string val)
    {
        lock (m_OptionFields)
        {
            if (!m_OptionFields.ContainsKey(nodeName)) return;

            var f = m_OptionFields[nodeName];
            f.SetValue(this, Helper.TranslateValue(f.FieldType, val));
            FlagDefined(f.Name);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Parses the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public override void Parse(XmlNode node)
    {
        if (node == null) throw new ArgumentNullException("node");

        foreach (XmlNode child in node.ChildNodes)
            SetOption(child.Name, Helper.InterpolateForEnvironmentVariables(child.InnerText));
    }

    public override void Write(XmlDocument doc, XmlElement current)
    {
        XmlElement options = doc.CreateElement("Options");
        foreach(var def in m_OptionFields.Keys)
        {
            var E = doc.CreateElement(def);
            E.InnerText = m_OptionFields[def].GetValue(this).ToString();

            options.AppendChild(E);
        }

        current.AppendChild(options);

    }

    /// <summary>
    ///     Return 'true' if the option symbol has had a value set
    /// </summary>
    /// <param name="name"></param>
    /// <returns>'true' if the option value has been set</returns>
    public bool IsDefined(string name)
    {
        return m_FieldsDefined.Contains("m_" + name);
    }

    // Return the names of all the fields that have had a value set
    public IEnumerable<string> AllDefined()
    {
        foreach (var name in m_FieldsDefined) yield return name.Substring(2);
    }

    /// <summary>
    ///     Copies to.
    /// </summary>
    /// <param name="opt">The opt.</param>
    public void CopyTo(OptionsNode opt)
    {
        if (opt == null) return;

        foreach (var f in m_OptionFields.Values)
            if (m_FieldsDefined.Contains(f.Name))
            {
                f.SetValue(opt, f.GetValue(this));
                opt.m_FieldsDefined.Add(f.Name);
            }
    }

    public override string ToString()
    {
        var buff = new StringBuilder();
        foreach (var f in m_OptionFields.Values)
            if (m_FieldsDefined.Contains(f.Name))
            {
                buff.Append(f.Name);
                buff.Append("=");
                buff.Append(f.GetValue(this));
                buff.Append(",");
            }

        return buff.ToString();
    }

    #endregion
}