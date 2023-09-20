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
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

/// <summary>
///     A set of values that the Project's type can be
/// </summary>
public enum ProjectType
{
    /// <summary>
    ///     The project is a console executable
    /// </summary>
    Exe,

    /// <summary>
    ///     The project is a windows executable
    /// </summary>
    WinExe,

    /// <summary>
    ///     The project is a library
    /// </summary>
    Library,

    /// <summary>
    ///     The project is a website
    /// </summary>
    Web
}

/// <summary>
/// </summary>
public enum ClrRuntime
{
    /// <summary>
    /// </summary>
    Microsoft,

    /// <summary>
    /// </summary>
    Mono
}

/// <summary>
///     The version of the .NET framework to use (Required for VS2008)
///     <remarks>We don't need .NET 1.1 in here, it'll default when using vs2003.</remarks>
/// </summary>
public enum FrameworkVersion : byte
{
    none = 0,

    /// <summary>
    ///     .NET 2.0
    /// </summary>
    v2_0,

    /// <summary>
    ///     .NET 3.0
    /// </summary>
    v3_0,

    /// <summary>
    ///     .NET 3.5
    /// </summary>
    v3_5,

    /// <summary>
    ///     .NET 4.0
    /// </summary>
    v4_0,

    /// <summary>
    ///     .NET 4.5
    /// </summary>
    v4_5,

    /// <summary>
    ///     .NET 4.5.1
    /// </summary>
    v4_5_1,
    v4_5_2,

    /// <summary>
    ///     .NET 4.6
    /// </summary>
    v4_6,

    /// <summary>
    ///     .NET 4.6.1
    /// </summary>
    v4_6_1,
    v4_6_2,
    v4_7,
    v4_7_1,
    v4_7_2,
    v4_8,
    netstandard2_0,
    net5_0,
    net6_0,
    net7_0
}

/// <summary>
///     The Node object representing /Prebuild/Solution/Project elements
/// </summary>
[DataNode("Project")]
public class ProjectNode : DataNode, IComparable
{
    #region IComparable Members

    public int CompareTo(object obj)
    {
        var that = (ProjectNode)obj;
        return Name.CompareTo(that.Name);
    }

    #endregion

    #region Private Methods

    private void HandleConfiguration(ConfigurationNode conf)
    {
        if (string.Compare(conf.Name, "all", true) == 0) //apply changes to all, this may not always be applied first,
            //so it *may* override changes to the same properties for configurations defines at the project level
            foreach (var confNode in ConfigurationsTable.Values)
                conf.CopyTo(confNode); //update the config templates defines at the project level with the overrides
        if (ConfigurationsTable.ContainsKey(conf.NameAndPlatform))
        {
            var parentConf = ConfigurationsTable[conf.NameAndPlatform];
            conf.CopyTo(parentConf); //update the config templates defines at the project level with the overrides
        }
        else
        {
            ConfigurationsTable[conf.NameAndPlatform] = conf;
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
        Name = Helper.AttributeValue(node, "name", Name);
        Path = Helper.AttributeValue(node, "path", Path);
        FilterGroups = Helper.AttributeValue(node, "filterGroups", FilterGroups);
        Version = Helper.AttributeValue(node, "version", Version);
        AppIcon = Helper.AttributeValue(node, "icon", AppIcon);
        ApplicationManifest = Helper.AttributeValue(node, "appmanifest", ApplicationManifest);
        ConfigFile = Helper.AttributeValue(node, "configFile", ConfigFile);
        DesignerFolder = Helper.AttributeValue(node, "designerFolder", DesignerFolder);
        AssemblyName = Helper.AttributeValue(node, "assemblyName", AssemblyName);
        ScanFiles = Helper.ParseBoolean(node, "scanFiles", true);
        Language = Helper.AttributeValue(node, "language", Language);
        Type = (ProjectType)Helper.EnumAttributeValue(node, "type", typeof(ProjectType), Type);
        Runtime = (ClrRuntime)Helper.EnumAttributeValue(node, "runtime", typeof(ClrRuntime), Runtime);
        if (m_useFramework)
            m_Framework =
                (FrameworkVersion)Helper.EnumAttributeValue(node, "frameworkVersion", typeof(FrameworkVersion),
                    m_Framework);

        m_Framework =
            (FrameworkVersion)Helper.EnumAttributeValue(node, "forceFrameworkVersion", typeof(FrameworkVersion),
                m_Framework);
        StartupObject = Helper.AttributeValue(node, "startupObject", StartupObject);
        RootNamespace = Helper.AttributeValue(node, "rootNamespace", RootNamespace);
        CopyLocalLockFileAssemblies =
            Helper.ParseBoolean(node, "copyDependencies", true); // This gives us legacy behavior.

        var hash = Name.GetHashCode();
        var guidByHash = new Guid(hash, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var guid = Helper.AttributeValue(node, "guid", guidByHash.ToString());
        Guid = new Guid(guid);

        GenerateAssemblyInfoFile = Helper.ParseBoolean(node, "generateAssemblyInfoFile", false);
        UseWindowsForms = Helper.ParseBoolean(node, "winforms", false);
        DebugStartParameters = Helper.AttributeValue(node, "debugStartParameters", string.Empty);

        if (string.IsNullOrEmpty(AssemblyName)) AssemblyName = Name;

        if (string.IsNullOrEmpty(RootNamespace)) RootNamespace = Name;

        FullPath = Path;
        try
        {
            FullPath = Helper.ResolvePath(FullPath);
        }
        catch
        {
            throw new WarningException("Could not resolve Solution path: {0}", Path);
        }

        Kernel.Instance.CurrentWorkingDirectory.Push();
        try
        {
            Helper.SetCurrentDir(FullPath);

            if (node == null) throw new ArgumentNullException("node");

            foreach (XmlNode child in node.ChildNodes)
            {
                var dataNode = Kernel.Instance.ParseNode(child, this);
                if (dataNode is ConfigurationNode)
                    HandleConfiguration((ConfigurationNode)dataNode);
                else if (dataNode is ReferencePathNode)
                    m_ReferencePaths.Add((ReferencePathNode)dataNode);
                else if (dataNode is ReferenceNode)
                    m_References.Add((ReferenceNode)dataNode);
                else if (dataNode is PackageReferenceNode)
                    m_PackageReferences.Add((PackageReferenceNode)dataNode);
                else if (dataNode is ProjectReferenceNode)
                    m_ProjectReferences.Add((ProjectReferenceNode)dataNode);
                else if (dataNode is AuthorNode)
                    Authors.Add((AuthorNode)dataNode);
                else if (dataNode is FilesNode) Files = (FilesNode)dataNode;
                else if (dataNode is TextGenNode) TextGenNodes.Add((TextGenNode)dataNode);
                else if (dataNode is MauiNode obj) MauiSettings = obj;
                else if (dataNode is NullableNode) Nullable = true;
                else if (dataNode is InternalsNode inObj) InternalsVisible = inObj; 
            }
        }
        finally
        {
            Kernel.Instance.CurrentWorkingDirectory.Pop();
        }
    }

    public override void Write(XmlDocument doc, XmlElement current)
    {
        XmlElement proj = doc.CreateElement("Project");

        proj.SetAttribute("name", Name);
        proj.SetAttribute("path", Path);
        proj.SetAttribute("filterGroups", FilterGroups);
        proj.SetAttribute("version", Version);
        proj.SetAttribute("icon", AppIcon);
        proj.SetAttribute("appmanifest", ApplicationManifest);
        proj.SetAttribute("configFile", ConfigFile);
        proj.SetAttribute("designerFolder", DesignerFolder);
        proj.SetAttribute("assemblyName", AssemblyName);
        proj.SetAttribute("scanFiles", ScanFiles ? bool.TrueString : bool.FalseString);
        proj.SetAttribute("language", Language);
        proj.SetAttribute("type", Type.ToString());
        proj.SetAttribute("runtime", Runtime.ToString());
        proj.SetAttribute("frameworkVersion", m_Framework.ToString());
        proj.SetAttribute("startupObject", StartupObject);
        proj.SetAttribute("rootNamespace", RootNamespace);
        proj.SetAttribute("copyDependencies", CopyLocalLockFileAssemblies ? bool.TrueString : bool.FalseString);
        proj.SetAttribute("guid", Guid.ToString());
        proj.SetAttribute("generateAssemblyInfoFile", GenerateAssemblyInfoFile ? bool.TrueString : bool.FalseString);
        proj.SetAttribute("winforms", UseWindowsForms ?  bool.TrueString : bool.FalseString);
        proj.SetAttribute("debugStartParameters", DebugStartParameters);

        foreach(ConfigurationNode conf in Configurations)
        {
            conf.Write(doc, proj);
        }

        foreach(ReferencePathNode rpn in ReferencePaths)
        {
            rpn.Write(doc, proj);
        }

        foreach(ReferenceNode refer in References)
        {
            refer.Write(doc, proj);
        }

        foreach(PackageReferenceNode pkg in PackageReferences)
        {
            pkg.Write(doc, proj);
        }

        foreach(ProjectReferenceNode prj in ProjectReferences)
        {
            prj.Write(doc, proj);
        }

        foreach(AuthorNode auth in Authors)
        {
            auth.Write(doc, proj);
        }

        Files.Write(doc, proj);
        foreach(TextGenNode gen in TextGenNodes)
        {
            gen.Write(doc, proj);
        }

        if(MauiSettings!=null)
            MauiSettings.Write(doc, proj);
        if (Nullable)
        {
            XmlElement nu = doc.CreateElement("Nullable");
            proj.AppendChild(nu);
        }

        if(InternalsVisible!=null)
            InternalsVisible.Write(doc, proj);




        current.AppendChild(proj);
    }

    #endregion

    #region Fields

    private FrameworkVersion m_Framework = FrameworkVersion.none;
    private bool m_useFramework = true;

    private readonly List<ReferencePathNode> m_ReferencePaths = new();
    private readonly List<ProjectReferenceNode> m_ProjectReferences = new();
    private readonly List<PackageReferenceNode> m_PackageReferences = new();
    private readonly List<ReferenceNode> m_References = new();

    private readonly Dictionary<FrameworkVersion, string> m_frameworkVersionToCondionalVersion = new()
    {
        { FrameworkVersion.v2_0, "NET20" },
        { FrameworkVersion.v3_0, "NET30" },
        { FrameworkVersion.v3_5, "NET35" },
        { FrameworkVersion.v4_0, "NET40" },
        { FrameworkVersion.v4_5, "NET45" },
        { FrameworkVersion.v4_5_1, "NET451" },
        { FrameworkVersion.v4_5_2, "NET452" },
        { FrameworkVersion.v4_6, "NET46" },
        { FrameworkVersion.v4_6_1, "NET461" },
        { FrameworkVersion.v4_6_2, "NET462" },
        { FrameworkVersion.v4_7, "NET47" },
        { FrameworkVersion.v4_7_1, "NET471" },
        { FrameworkVersion.v4_7_2, "NET472" },
        { FrameworkVersion.v4_8, "NET48" },
        { FrameworkVersion.netstandard2_0, "NETSTANDARD2_0" },
        { FrameworkVersion.net5_0, "NET5_0" },
        { FrameworkVersion.net6_0, "NET6_0" },
        { FrameworkVersion.net7_0, "NET7_0" }
    };

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; internal set; } = "unknown";

    /// <summary>
    /// Contains settings for DotNet Maui (7.0+)
    /// Default is null, which indicates not to include any Maui content in the project file.
    /// </summary>
    public MauiNode MauiSettings { get; internal set; } = null;

    /// <summary>
    /// Marks the visibility for internals
    /// </summary>
    public InternalsNode InternalsVisible { get; internal set; }

    /// <summary>
    /// Enables Windows forms on a dotnet project.
    /// </summary>
    public bool UseWindowsForms { get; internal set; } = false;

    /// <summary>
    /// Scans the directory for files
    /// </summary>
    public bool ScanFiles {get;internal set;} = true;

    /// <summary>
    ///     The version of the .NET Framework to compile under
    /// </summary>
    public FrameworkVersion FrameworkVersion
    {
        get => m_Framework;
        set
        {
            m_Framework = value;
            m_useFramework = false;
        }
    }

    public string FrameworkVersionForConditional
    {
        get
        {
            if (m_frameworkVersionToCondionalVersion.TryGetValue(m_Framework, out var ret))
                return ret;
            return string.Empty;
        }
    }

    /// <summary>
    ///     Gets the path.
    /// </summary>
    /// <value>The path.</value>
    public string Path { get; internal set; } = "";

    /// <summary>
    ///     Gets the filter groups.
    /// </summary>
    /// <value>The filter groups.</value>
    public string FilterGroups { get; internal set; } = "";

    /// <summary>
    /// Indicates project nullable attribute
    /// </summary>
    public bool Nullable { get; internal set; } = false;
    public string NullableStr 
    { 
        get
        {
            return Nullable ? "enable" : "disable";
        } 
    }

    /// <summary>
    ///     Gets the project's version
    /// </summary>
    /// <value>The project's version.</value>
    public string Version { get; internal set; } = "";

    /// <summary>
    ///     Gets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath { get; internal set; } = "";

    /// <summary>
    ///     Gets the name of the assembly.
    /// </summary>
    /// <value>The name of the assembly.</value>
    public string AssemblyName { get; internal set; }

    /// <summary>
    ///     Copies the local dependencies to the output on build.
    ///     This is the same behavior as publish.
    /// </summary>
    public bool CopyLocalLockFileAssemblies { get; internal set; }

    /// <summary>
    ///     Gets the app icon.
    /// </summary>
    /// <value>The app icon.</value>
    public string AppIcon { get; internal set; } = "";

    /// <summary>
    ///     Gets the Application Manifest.
    /// </summary>
    /// <value>The Application Manifest.</value>
    public string ApplicationManifest { get; internal set; } = "";

    /// <summary>
    ///     Gets the app icon.
    /// </summary>
    /// <value>The app icon.</value>
    public string ConfigFile { get; internal set; } = "";

    /// <summary>
    /// </summary>
    public string DesignerFolder { get; internal set; } = "";

    /// <summary>
    ///     Gets the language.
    /// </summary>
    /// <value>The language.</value>
    public string Language { get; internal set; } = "C#";

    /// <summary>
    ///     Gets the type.
    /// </summary>
    /// <value>The type.</value>
    public ProjectType Type { get; internal set; } = ProjectType.Exe;

    /// <summary>
    ///     Gets the runtime.
    /// </summary>
    /// <value>The runtime.</value>
    public ClrRuntime Runtime { get; internal set; } = ClrRuntime.Microsoft;

    /// <summary>
    /// </summary>
    public bool GenerateAssemblyInfoFile { get; set; }

    /// <summary>
    ///     Gets the startup object.
    /// </summary>
    /// <value>The startup object.</value>
    public string StartupObject { get; internal set; } = "";

    /// <summary>
    ///     Gets the root namespace.
    /// </summary>
    /// <value>The root namespace.</value>
    public string RootNamespace { get; internal set; }

    /// <summary>
    ///     Gets the configurations.
    /// </summary>
    /// <value>The configurations.</value>
    public List<ConfigurationNode> Configurations
    {
        get
        {
            var tmp = new List<ConfigurationNode>(ConfigurationsTable.Values);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    /// Gets the text generator nodes
    /// </summary>
    public List<TextGenNode> TextGenNodes { get; internal set; } = new();


    /// <summary>
    ///     Gets the configurations table.
    /// </summary>
    /// <value>The configurations table.</value>
    public Dictionary<string, ConfigurationNode> ConfigurationsTable { get; } = new();

    /// <summary>
    ///     Gets the reference paths.
    /// </summary>
    /// <value>The reference paths.</value>
    public List<ReferencePathNode> ReferencePaths
    {
        get
        {
            var tmp = new List<ReferencePathNode>(m_ReferencePaths);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the references.
    /// </summary>
    /// <value>The references.</value>
    public List<ReferenceNode> References
    {
        get
        {
            var tmp = new List<ReferenceNode>(m_References);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the references.
    /// </summary>
    /// <value>The references.</value>
    public List<ProjectReferenceNode> ProjectReferences
    {
        get
        {
            var tmp = new List<ProjectReferenceNode>(m_ProjectReferences);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the package references.
    /// </summary>
    /// <value>The references.</value>
    public List<PackageReferenceNode> PackageReferences
    {
        get
        {
            var tmp = new List<PackageReferenceNode>(m_PackageReferences);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the Authors list.
    /// </summary>
    /// <value>The list of the project's authors.</value>
    public List<AuthorNode> Authors { get; } = new();

    /// <summary>
    ///     Gets the files.
    /// </summary>
    /// <value>The files.</value>
    public FilesNode Files { get; internal set; } = new();

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
            if (base.Parent is SolutionNode && ConfigurationsTable.Count < 1)
            {
                var parent = (SolutionNode)base.Parent;
                foreach (var conf in parent.Configurations)
                    ConfigurationsTable[conf.NameAndPlatform] = (ConfigurationNode)conf.Clone();
                if (m_useFramework)
                {
                    if (parent.ForceFramework != FrameworkVersion.none)
                    {
                        m_Framework = parent.ForceFramework;
                        m_useFramework = false;
                    }
                    else if (m_Framework == FrameworkVersion.none)
                    {
                        m_Framework = parent.DefaultFramework != FrameworkVersion.none
                            ? parent.DefaultFramework
                            : FrameworkVersion.v2_0;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Gets the GUID.
    /// </summary>
    /// <value>The GUID.</value>
    public Guid Guid { get; internal set; }

    public string DebugStartParameters { get; internal set; }

    #endregion
}