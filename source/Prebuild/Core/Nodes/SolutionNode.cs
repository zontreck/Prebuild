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
using System.Diagnostics;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
[DataNode("Solution")]
[DataNode("EmbeddedSolution")]
[DebuggerDisplay("{Name}")]
public class SolutionNode : DataNode
{
    #region Public Methods

    /// <summary>
    ///     Parses the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public override void Parse(XmlNode node)
    {
        Name = Helper.AttributeValue(node, "name", Name);
        ActiveConfig = Helper.AttributeValue(node, "activeConfig", ActiveConfig);
        Path = Helper.AttributeValue(node, "path", Path);
        Version = Helper.AttributeValue(node, "version", Version);
        var tmp = Helper.AttributeValue(node, "forceFrameworkVersion", "");
        if (tmp.Length > 0)
            if (!Enum.TryParse(tmp, true, out ForceFramework))
                ForceFramework = FrameworkVersion.none;

        if (ForceFramework == FrameworkVersion.none)
        {
            tmp = Helper.AttributeValue(node, "frameworkVersion", "");
            if (tmp.Length > 0)
                if (!Enum.TryParse(tmp, true, out DefaultFramework))
                    DefaultFramework = FrameworkVersion.none;
        }

        FullPath = Path;
        try
        {
            FullPath = Helper.ResolvePath(FullPath);
        }
        catch
        {
            throw new WarningException("Could not resolve solution path: {0}", Path);
        }


        Kernel.Instance.CurrentWorkingDirectory.Push();
        try
        {
            Helper.SetCurrentDir(FullPath);

            if (node == null) throw new ArgumentNullException("node");

            foreach (XmlNode child in node.ChildNodes)
            {
                var dataNode = Kernel.Instance.ParseNode(child, this);
                if (dataNode is OptionsNode)
                {
                    Options = (OptionsNode)dataNode;
                }
                else if (dataNode is FilesNode)
                {
                    Files = (FilesNode)dataNode;
                }
                else if (dataNode is ConfigurationNode)
                {
                    var configurationNode = (ConfigurationNode)dataNode;
                    ConfigurationsTable[configurationNode.NameAndPlatform] = configurationNode;

                    // If the active configuration is null, then we populate it.
                    if (ActiveConfig == null) ActiveConfig = configurationNode.Name;
                }
                else if (dataNode is ProjectNode)
                {
                    ProjectsTable[((ProjectNode)dataNode).Name] = (ProjectNode)dataNode;
                    ProjectsTableOrder.Add((ProjectNode)dataNode);
                }
                else if (dataNode is SolutionNode)
                {
                    SolutionsTable[((SolutionNode)dataNode).Name] = (SolutionNode)dataNode;
                }
                else if (dataNode is ProcessNode)
                {
                    var p = (ProcessNode)dataNode;
                    Kernel.Instance.ProcessFile(p, this);
                }
                else if (dataNode is DatabaseProjectNode)
                {
                    m_DatabaseProjects[((DatabaseProjectNode)dataNode).Name] = (DatabaseProjectNode)dataNode;
                }
                else if (dataNode is CleanupNode)
                {
                    if (Cleanup != null)
                        throw new WarningException("There can only be one Cleanup node.");
                    Cleanup = (CleanupNode)dataNode;
                }
            }
        }
        finally
        {
            Kernel.Instance.CurrentWorkingDirectory.Pop();
        }
    }

    #endregion

    #region Fields

    public FrameworkVersion DefaultFramework = FrameworkVersion.none;
    public FrameworkVersion ForceFramework = FrameworkVersion.none;

    private readonly Dictionary<string, DatabaseProjectNode> m_DatabaseProjects = new();

    #endregion

    #region Properties

    public override IDataNode Parent
    {
        get => base.Parent;
        set
        {
            if (value is SolutionNode)
            {
                var solution = (SolutionNode)value;
                foreach (var conf in solution.Configurations)
                    ConfigurationsTable[conf.Name] = (ConfigurationNode)conf.Clone();
            }

            base.Parent = value;
        }
    }

    public CleanupNode Cleanup { get; set; }

    public Guid Guid { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the active config.
    /// </summary>
    /// <value>The active config.</value>
    public string ActiveConfig { get; set; }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; private set; } = "unknown";

    /// <summary>
    ///     Gets the path.
    /// </summary>
    /// <value>The path.</value>
    public string Path { get; private set; } = "";

    /// <summary>
    ///     Gets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath { get; private set; } = "";

    /// <summary>
    ///     Gets the version.
    /// </summary>
    /// <value>The version.</value>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>
    ///     Gets the options.
    /// </summary>
    /// <value>The options.</value>
    public OptionsNode Options { get; private set; }

    /// <summary>
    ///     Gets the files.
    /// </summary>
    /// <value>The files.</value>
    public FilesNode Files { get; private set; }

    /// <summary>
    ///     Gets the configurations.
    /// </summary>
    /// <value>The configurations.</value>
    public ConfigurationNodeCollection Configurations
    {
        get
        {
            var tmp = new ConfigurationNodeCollection();
            tmp.AddRange(ConfigurationsTable);
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the configurations table.
    /// </summary>
    /// <value>The configurations table.</value>
    public ConfigurationNodeCollection ConfigurationsTable { get; } = new();

    /// <summary>
    ///     Gets the database projects.
    /// </summary>
    public ICollection<DatabaseProjectNode> DatabaseProjects => m_DatabaseProjects.Values;

    /// <summary>
    ///     Gets the nested solutions.
    /// </summary>
    public ICollection<SolutionNode> Solutions => SolutionsTable.Values;

    /// <summary>
    ///     Gets the nested solutions hash table.
    /// </summary>
    public Dictionary<string, SolutionNode> SolutionsTable { get; } = new();

    /// <summary>
    ///     Gets the projects.
    /// </summary>
    /// <value>The projects.</value>
    public ICollection<ProjectNode> Projects
    {
        get
        {
            var tmp = new List<ProjectNode>(ProjectsTable.Values);
            tmp.Sort();
            return tmp;
        }
    }

    /// <summary>
    ///     Gets the projects table.
    /// </summary>
    /// <value>The projects table.</value>
    public Dictionary<string, ProjectNode> ProjectsTable { get; } = new();

    /// <summary>
    ///     Gets the projects table.
    /// </summary>
    /// <value>The projects table.</value>
    public List<ProjectNode> ProjectsTableOrder { get; } = new();

    #endregion
}