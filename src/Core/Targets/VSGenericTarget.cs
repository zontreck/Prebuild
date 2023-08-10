#region BSD License

/*
Copyright (c) 2008 Matthew Holmes (matthew@wildfiregames.com), John Anderson (sontek@gmail.com)

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

using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Utilities;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Prebuild.Core.Targets;

/// <summary>
/// </summary>
public abstract class VSGenericTarget : ITarget
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="VSGenericTarget" /> class.
    /// </summary>
    protected VSGenericTarget()
    {
        tools["C#"] = new ToolInfo("C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "csproj", "CSHARP",
            "$(MSBuildBinPath)\\Microsoft.CSharp.targets");
        tools["Database"] = new ToolInfo("Database", "{4F174C21-8C12-11D0-8340-0000F80270F8}", "dbp", "UNKNOWN");
        tools["Boo"] = new ToolInfo("Boo", "{45CEA7DC-C2ED-48A6-ACE0-E16144C02365}", "booproj", "Boo",
            "$(BooBinPath)\\Boo.Microsoft.Build.targets");
        tools["VisualBasic"] = new ToolInfo("VisualBasic", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "vbproj",
            "VisualBasic", "$(MSBuildBinPath)\\Microsoft.VisualBasic.Targets");
        tools["Folder"] = new ToolInfo("Folder", "{2150E333-8FDC-42A3-9474-1A3956D46DE8}", null, null);
    }

    #endregion

    #region Fields

    private readonly Dictionary<string, ToolInfo> tools = new();

    //        NameValueCollection CopyFiles = new NameValueCollection();
    private Kernel kernel;

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the solution version.
    /// </summary>
    /// <value>The solution version.</value>
    public abstract string SolutionVersion { get; }

    /// <summary>
    ///     Gets or sets the product version.
    /// </summary>
    /// <value>The product version.</value>
    public abstract string ProductVersion { get; }

    /// <summary>
    ///     Gets or sets the schema version.
    /// </summary>
    /// <value>The schema version.</value>
    public abstract string SchemaVersion { get; }

    /// <summary>
    ///     Gets or sets the name of the version.
    /// </summary>
    /// <value>The name of the version.</value>
    public abstract string VersionName { get; }

    /// <summary>
    ///     Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public abstract VSVersion Version { get; }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public abstract string Name { get; }

    protected abstract string GetToolsVersionXml(FrameworkVersion version);
    public abstract string SolutionTag { get; }

    #endregion

    #region Private Methods

    private string MakeRefPath(ProjectNode project)
    {
        var ret = "";
        foreach (var node in project.ReferencePaths)
            try
            {
                var fullPath = Helper.ResolvePath(node.Path);
                if (ret.Length < 1)
                    ret = fullPath;
                else
                    ret += ";" + fullPath;
            }
            catch (ArgumentException)
            {
                kernel.Log.Write(LogType.Warning, "Could not resolve reference path: {0}", node.Path);
            }

        return ret;
    }

    private static ProjectNode FindProjectInSolution(string name, SolutionNode solution)
    {
        var node = solution;

        while (node.Parent is SolutionNode)
            node = node.Parent as SolutionNode;

        return FindProjectInSolutionRecursively(name, node);
    }

    private static ProjectNode FindProjectInSolutionRecursively(string name, SolutionNode solution)
    {
        if (solution.ProjectsTable.ContainsKey(name))
            return solution.ProjectsTable[name];

        foreach (var child in solution.Solutions)
        {
            var node = FindProjectInSolutionRecursively(name, child);
            if (node != null)
                return node;
        }

        return null;
    }

    private void WriteProject(SolutionNode solution, ProjectNode project)
    {
        if (!tools.ContainsKey(project.Language))
            throw new UnknownLanguageException("Unknown .NET language: " + project.Language);

        var toolInfo = tools[project.Language];
        var projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
        var ps = new StreamWriter(projectFile);

        kernel.CurrentWorkingDirectory.Push();
        Helper.SetCurrentDir(Path.GetDirectoryName(projectFile));

        var fv = project.FrameworkVersion;
        if (fv > FrameworkVersion.v4_8)
            // Write the newer .csproj file format
            WriteProjectDotNet(solution, project, ps);
        else
            WriteProjectFramework(solution, project, toolInfo, projectFile, ps);

        kernel.CurrentWorkingDirectory.Pop();
    }

    private void WriteProjectFramework(SolutionNode solution, ProjectNode project, ToolInfo toolInfo,
        string projectFile, StreamWriter ps)
    {
        #region Project File

        using (ps)
        {
            var targets = "";

            if (project.Files.CopyFiles > 0)
                targets = "Build;CopyFiles";
            else
                targets = "Build";

            ps.WriteLine(
                "<Project DefaultTargets=\"{0}\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" {1}>",
                targets, GetToolsVersionXml(project.FrameworkVersion));
            ps.WriteLine("	<PropertyGroup>");
            ps.WriteLine("	  <ProjectType>Local</ProjectType>");
            ps.WriteLine("	  <ProductVersion>{0}</ProductVersion>", ProductVersion);
            ps.WriteLine("	  <SchemaVersion>{0}</SchemaVersion>", SchemaVersion);
            ps.WriteLine("	  <ProjectGuid>{{{0}}}</ProjectGuid>", project.Guid.ToString().ToUpper());

            // Visual Studio has a hard coded guid for the project type
            if (project.Type == ProjectType.Web)
                ps.WriteLine(
                    "	  <ProjectTypeGuids>{349c5851-65df-11da-9384-00065b846f21};{fae04ec0-301f-11d3-bf4b-00c04f79efbc}</ProjectTypeGuids>");
            ps.WriteLine("	  <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
            ps.WriteLine("	  <ApplicationIcon>{0}</ApplicationIcon>", project.AppIcon);
            ps.WriteLine("	  <AssemblyKeyContainerName>");
            ps.WriteLine("	  </AssemblyKeyContainerName>");
            ps.WriteLine("	  <AssemblyName>{0}</AssemblyName>", project.AssemblyName);
            foreach (var conf in project.Configurations)
                if (conf.Options.KeyFile != "")
                {
                    ps.WriteLine("	  <AssemblyOriginatorKeyFile>{0}</AssemblyOriginatorKeyFile>", conf.Options.KeyFile);
                    ps.WriteLine("	  <SignAssembly>true</SignAssembly>");
                    break;
                }

            ps.WriteLine("	  <DefaultClientScript>JScript</DefaultClientScript>");
            ps.WriteLine("	  <DefaultHTMLPageLayout>Grid</DefaultHTMLPageLayout>");
            ps.WriteLine("	  <DefaultTargetSchema>IE50</DefaultTargetSchema>");
            ps.WriteLine("	  <DelaySign>false</DelaySign>");
            ps.WriteLine("	  <TargetFrameworkVersion>{0}</TargetFrameworkVersion>",
                project.FrameworkVersion.ToString().Replace("_", "."));

            ps.WriteLine("	  <OutputType>{0}</OutputType>",
                project.Type == ProjectType.Web ? ProjectType.Library.ToString() : project.Type.ToString());
            ps.WriteLine("	  <AppDesignerFolder>{0}</AppDesignerFolder>", project.DesignerFolder);
            ps.WriteLine("	  <RootNamespace>{0}</RootNamespace>", project.RootNamespace);
            ps.WriteLine("	  <StartupObject>{0}</StartupObject>", project.StartupObject);
            if (string.IsNullOrEmpty(project.DebugStartParameters))
                ps.WriteLine("	  <StartArguments>{0}</StartArguments>", project.DebugStartParameters);
            ps.WriteLine("	  <FileUpgradeFlags>");
            ps.WriteLine("	  </FileUpgradeFlags>");

            ps.WriteLine("	</PropertyGroup>");
            if (!string.IsNullOrEmpty(project.ApplicationManifest))
            {
                ps.WriteLine("	<PropertyGroup>");
                ps.WriteLine("	  <ApplicationManifest>" + project.ApplicationManifest + "</ApplicationManifest>");
                ps.WriteLine("	</PropertyGroup>");
            }

            foreach (var conf in project.Configurations)
            {
                ps.Write("	<PropertyGroup ");
                ps.WriteLine("Condition=\" '$(Configuration)|$(Platform)' == '{0}|{1}' \">", conf.Name, conf.Platform);
                ps.WriteLine("	  <AllowUnsafeBlocks>{0}</AllowUnsafeBlocks>", conf.Options["AllowUnsafe"]);
                ps.WriteLine("	  <BaseAddress>{0}</BaseAddress>", conf.Options["BaseAddress"]);
                ps.WriteLine("	  <CheckForOverflowUnderflow>{0}</CheckForOverflowUnderflow>",
                    conf.Options["CheckUnderflowOverflow"]);
                ps.WriteLine("	  <ConfigurationOverrideFile>");
                ps.WriteLine("	  </ConfigurationOverrideFile>");
                var defConstants = conf.Options["CompilerDefines"].ToString() == ""
                    ? kernel.ForcedConditionals
                    : conf.Options["CompilerDefines"] + ";" + kernel.ForcedConditionals;
                if (!string.IsNullOrEmpty(project.FrameworkVersionForConditional))
                {
                    if (string.IsNullOrEmpty(defConstants))
                        defConstants = project.FrameworkVersionForConditional + ";";
                    else if (defConstants.IndexOf(project.FrameworkVersionForConditional) < 0)
                        defConstants += project.FrameworkVersionForConditional + ";";
                }

                if (!string.IsNullOrEmpty(defConstants))
                    ps.WriteLine("	  <DefineConstants>{0}</DefineConstants>", defConstants);

                ps.WriteLine("	  <DocumentationFile>{0}</DocumentationFile>",
                    Helper.NormalizePath(conf.Options["XmlDocFile"].ToString()));
                ps.WriteLine("	  <DebugSymbols>{0}</DebugSymbols>", conf.Options["DebugInformation"]);
                ps.WriteLine("	  <FileAlignment>{0}</FileAlignment>", conf.Options["FileAlignment"]);
                ps.WriteLine("	  <Optimize>{0}</Optimize>", conf.Options["OptimizeCode"]);
                if (project.Type != ProjectType.Web)
                    ps.WriteLine("	  <OutputPath>{0}</OutputPath>",
                        Helper.EndPath(Helper.NormalizePath(conf.Options["OutputPath"].ToString())));
                else
                    ps.WriteLine("	  <OutputPath>{0}</OutputPath>",
                        Helper.EndPath(Helper.NormalizePath("bin\\")));

                ps.WriteLine("	  <RegisterForComInterop>{0}</RegisterForComInterop>",
                    conf.Options["RegisterComInterop"]);
                ps.WriteLine("	  <RemoveIntegerChecks>{0}</RemoveIntegerChecks>", conf.Options["RemoveIntegerChecks"]);
                ps.WriteLine("	  <TreatWarningsAsErrors>{0}</TreatWarningsAsErrors>", conf.Options["WarningsAsErrors"]);
                ps.WriteLine("	  <WarningLevel>{0}</WarningLevel>", conf.Options["WarningLevel"]);
                ps.WriteLine("	  <NoStdLib>{0}</NoStdLib>", conf.Options["NoStdLib"]);
                ps.WriteLine("	  <NoWarn>{0}</NoWarn>", conf.Options["SuppressWarnings"]);
                ps.WriteLine("	  <PlatformTarget>{0}</PlatformTarget>", conf.Platform);
                ps.WriteLine("	  <Prefer32Bit>{0}</Prefer32Bit>", conf.Options["Prefer32Bit"]);
                ps.WriteLine("	</PropertyGroup>");
            }

            //ps.WriteLine("	  </Settings>");

            // Output warnings if NET6 stuff is in a Framework4 definition
            if (project.ProjectReferences.Count > 0)
                kernel.Log.Write(LogType.Warning, "ProjectReference is not processed for Frameworks.");
            if (project.PackageReferences.Count > 0)
                kernel.Log.Write(LogType.Warning, "PackageReference is not processed for Frameworks.");

            // Output the ItemGroup for project.References
            WriteProjectReferences(solution, project, ps);

            //ps.WriteLine("  </Build>");
            ps.WriteLine("  <ItemGroup>");

            //ps.WriteLine("  <Include>");
            var list = new List<string>();

            foreach (var path in project.Files)
            {
                var lower = path.ToLower();
                if (lower.EndsWith(".resx"))
                {
                    var codebehind = string.Format("{0}.Designer{1}", path.Substring(0, path.LastIndexOf('.')),
                        toolInfo.LanguageExtension);
                    if (!list.Contains(codebehind))
                        list.Add(codebehind);
                }
            }

            #region Files

            foreach (var filePath in project.Files)
            {
                // Add the filePath with the destination as the key
                // will use it later to form the copy parameters with Include lists
                // for each destination
                if (project.Files.GetBuildAction(filePath) == BuildAction.Copy)
                    continue;
                //if (file == "Properties\\Bind.Designer.cs")
                //{
                //    Console.WriteLine("Wait a minute!");
                //    Console.WriteLine(project.Files.GetSubType(file).ToString());
                //}
                var subType = project.Files.GetSubType(filePath);

                // Visual Studio chokes on file names if forward slash is used as a path separator
                // instead of backslash.  So we must make sure that all file paths written to the
                // project file use \ as a path separator.
                var file = filePath.Replace(@"/", @"\");

                if (subType != SubType.Code && subType != SubType.Settings && subType != SubType.Designer
                    && subType != SubType.CodeBehind)
                {
                    ps.WriteLine("    <EmbeddedResource Include=\"{0}\">",
                        file.Substring(0, file.LastIndexOf('.')) + ".resx");
                    ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(file));
                    ps.WriteLine("      <SubType>Designer</SubType>");
                    ps.WriteLine("    </EmbeddedResource>");
                    //
                }

                if (subType == SubType.Designer)
                {
                    ps.WriteLine("    <EmbeddedResource Include=\"{0}\">", file);

                    var autogen_name = file.Substring(0, file.LastIndexOf('.')) + ".Designer.cs";
                    var dependent_name = filePath.Substring(0, file.LastIndexOf('.')) + ".cs";

                    // Check for a parent .cs file with the same name as this designer file
                    if (File.Exists(Helper.NormalizePath(dependent_name)))
                    {
                        ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(dependent_name));
                    }
                    else
                    {
                        ps.WriteLine("      <Generator>ResXFileCodeGenerator</Generator>");
                        ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>", Path.GetFileName(autogen_name));
                        ps.WriteLine("      <SubType>" + subType + "</SubType>");
                    }

                    ps.WriteLine("    </EmbeddedResource>");
                    if (File.Exists(Helper.NormalizePath(autogen_name)))
                    {
                        ps.WriteLine("    <Compile Include=\"{0}\">", autogen_name);
                        //ps.WriteLine("      <DesignTime>True</DesignTime>");

                        // If a parent .cs file exists, link this autogen file to it. Otherwise link
                        // to the designer file
                        if (File.Exists(dependent_name))
                        {
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(dependent_name));
                        }
                        else
                        {
                            ps.WriteLine("      <AutoGen>True</AutoGen>");
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(filePath));
                        }

                        ps.WriteLine("    </Compile>");
                    }

                    list.Add(autogen_name);
                }

                if (subType == SubType.Settings)
                {
                    ps.Write("    <{0} ", project.Files.GetBuildAction(filePath));
                    ps.WriteLine("      Include=\"{0}\">", file);
                    var fileName = Path.GetFileName(filePath);
                    if (project.Files.GetBuildAction(filePath) == BuildAction.None)
                    {
                        ps.WriteLine("      <Generator>SettingsSingleFileGenerator</Generator>");
                        ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>",
                            fileName.Substring(0, fileName.LastIndexOf('.')) + ".Designer.cs");
                    }
                    else
                    {
                        ps.WriteLine("      <SubType>Code</SubType>");
                        ps.WriteLine("      <AutoGen>True</AutoGen>");
                        ps.WriteLine("      <DesignTimeSharedInput>True</DesignTimeSharedInput>");
                        var fileNameShort = fileName.Substring(0, fileName.LastIndexOf('.'));
                        var fileNameShorter = fileNameShort.Substring(0, fileNameShort.LastIndexOf('.'));
                        ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                            Path.GetFileName(fileNameShorter + ".settings"));
                    }

                    ps.WriteLine("    </{0}>", project.Files.GetBuildAction(filePath));
                }
                else if (subType != SubType.Designer)
                {
                    var path = Helper.NormalizePath(file);
                    var path_lower = path.ToLower();

                    if (!list.Contains(filePath))
                    {
                        ps.Write("    <{0} ", project.Files.GetBuildAction(filePath));

                        var startPos = 0;
                        if (project.Files.GetPreservePath(filePath))
                            while (@"./\".IndexOf(file.Substring(startPos, 1)) != -1)
                                startPos++;
                        else
                            startPos = file.LastIndexOf(Path.GetFileName(path));

                        // be sure to write out the path with backslashes so VS recognizes
                        // the file properly.
                        ps.WriteLine("    Include=\"{0}\">", file);

                        var last_period_index = file.LastIndexOf('.');
                        var short_file_name = last_period_index >= 0
                            ? file.Substring(0, last_period_index)
                            : file;
                        var extension = Path.GetExtension(path);
                        // make this upper case, so that when File.Exists tests for the
                        // existence of a designer file on a case-sensitive platform,
                        // it is correctly identified.
                        var designer_format = string.Format(".Designer{0}", extension);

                        if (path_lower.EndsWith(designer_format.ToLowerInvariant()))
                        {
                            var designer_index = path.IndexOf(designer_format);
                            var file_name = path.Substring(0, designer_index);

                            // There are two corrections to the next lines:
                            // 1. Fix the connection between a designer file and a form
                            //	  or usercontrol that don't have an associated resx file.
                            // 2. Connect settings files to associated designer files.
                            if (File.Exists(file_name + extension))
                            {
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                    Path.GetFileName(file_name + extension));
                            }
                            else if (File.Exists(file_name + ".resx"))
                            {
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                    Path.GetFileName(file_name + ".resx"));
                            }
                            else if (File.Exists(file_name + ".settings"))
                            {
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                    Path.GetFileName(file_name + ".settings"));
                                ps.WriteLine("      <AutoGen>True</AutoGen>");
                                ps.WriteLine("      <DesignTimeSharedInput>True</DesignTimeSharedInput>");
                            }
                        }
                        else if (subType == SubType.CodeBehind)
                        {
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(short_file_name));
                        }

                        if (project.Files.GetIsLink(filePath))
                        {
                            var alias = project.Files.GetLinkPath(filePath);
                            alias += file.Substring(startPos);
                            alias = Helper.NormalizePath(alias);
                            ps.WriteLine("      <Link>{0}</Link>", alias);
                        }
                        else if (project.Files.GetBuildAction(filePath) != BuildAction.None)
                        {
                            if (project.Files.GetBuildAction(filePath) != BuildAction.EmbeddedResource)
                                ps.WriteLine("      <SubType>{0}</SubType>", subType);
                        }

                        if (project.Files.GetCopyToOutput(filePath) != CopyToOutput.Never)
                            ps.WriteLine("      <CopyToOutputDirectory>{0}</CopyToOutputDirectory>",
                                project.Files.GetCopyToOutput(filePath));

                        ps.WriteLine("    </{0}>", project.Files.GetBuildAction(filePath));
                    }
                }
            }

            ps.WriteLine("  </ItemGroup>");

            #endregion

            /*
             * Copy Task
             *
            */
            if (project.Files.CopyFiles > 0)
            {
                var IncludeTags = new Dictionary<string, string>();
                var TagCount = 0;

                // Handle Copy tasks
                ps.WriteLine("  <ItemGroup>");
                foreach (var destPath in project.Files.Destinations)
                {
                    var tag = "FilesToCopy_" + TagCount.ToString("0000");

                    ps.WriteLine("    <{0} Include=\"{1}\" />", tag,
                        string.Join(";", project.Files.SourceFiles(destPath)));
                    IncludeTags.Add(destPath, tag);
                    TagCount++;
                }

                ps.WriteLine("  </ItemGroup>");

                ps.WriteLine("  <Target Name=\"CopyFiles\" AfterTargets=\"AfterBuild\">");

                foreach (var destPath in project.Files.Destinations)
                    ps.WriteLine("    <Copy SourceFiles=\"@({0})\" DestinationFolder=\"{1}\" />",
                        IncludeTags[destPath], destPath);

                ps.WriteLine("  </Target>");
            }

            ps.WriteLine("  <Import Project=\"" + toolInfo.ImportProject + "\" />");
            ps.WriteLine("  <PropertyGroup>");
            ps.WriteLine("    <PreBuildEvent>");
            ps.WriteLine("    </PreBuildEvent>");
            ps.WriteLine("    <PostBuildEvent>");
            ps.WriteLine("    </PostBuildEvent>");
            ps.WriteLine("  </PropertyGroup>");
            ps.WriteLine("</Project>");
        }

        #endregion

        #region User File

        ps = new StreamWriter(projectFile + ".user");
        using (ps)
        {
            // Get the first configuration from the project.
            ConfigurationNode firstConfiguration = null;

            if (project.Configurations.Count > 0) firstConfiguration = project.Configurations[0];

            ps.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            //ps.WriteLine( "<VisualStudioProject>" );
            //ps.WriteLine("  <{0}>", toolInfo.XMLTag);
            //ps.WriteLine("	<Build>");
            ps.WriteLine("	<PropertyGroup>");
            //ps.WriteLine("	  <Settings ReferencePath=\"{0}\">", MakeRefPath(project));

            if (firstConfiguration != null)
            {
                ps.WriteLine("	  <Configuration Condition=\" '$(Configuration)' == '' \">{0}</Configuration>",
                    firstConfiguration.Name);
                ps.WriteLine("	  <Platform Condition=\" '$(Platform)' == '' \">{0}</Platform>",
                    firstConfiguration.Platform);
            }

            ps.WriteLine("	  <ReferencePath>{0}</ReferencePath>", MakeRefPath(project));
            ps.WriteLine("	  <LastOpenVersion>{0}</LastOpenVersion>", ProductVersion);
            ps.WriteLine("	  <ProjectView>ProjectFiles</ProjectView>");
            ps.WriteLine("	  <ProjectTrust>0</ProjectTrust>");
            ps.WriteLine("	</PropertyGroup>");
            foreach (var conf in project.Configurations)
            {
                ps.Write("	<PropertyGroup");
                ps.Write(" Condition = \" '$(Configuration)|$(Platform)' == '{0}|{1}' \"", conf.Name, conf.Platform);
                ps.WriteLine(" />");
            }

            ps.WriteLine("</Project>");
        }

        #endregion
    }

    private void WriteProjectReferences(SolutionNode solution, ProjectNode project, StreamWriter ps)
    {
        var projectReferences = new Dictionary<ReferenceNode, ProjectNode>();
        var otherReferences = new List<ReferenceNode>();

        foreach (var refr in project.References)
        {
            var projectNode = FindProjectInSolution(refr.Name, solution);

            if (projectNode == null)
                otherReferences.Add(refr);
            else
                projectReferences.Add(refr, projectNode);
        }

        // Assembly References
        if (otherReferences.Count > 0)
        {
            ps.WriteLine("  <ItemGroup>");

            foreach (var refr in otherReferences)
            {
                ps.Write("    <Reference");
                ps.Write(" Include=\"");
                ps.Write(refr.Name);
                ps.WriteLine("\" >");
                ps.Write("    <Name>");
                ps.Write(refr.Name);
                ps.WriteLine("</Name>");

                if (!string.IsNullOrEmpty(refr.Path))
                {
                    // Use absolute path to assembly (for determining assembly type)
                    var absolutePath = Path.Combine(project.FullPath, refr.Path);
                    if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "exe")))
                    {
                        // Assembly is an executable (exe)
                        ps.WriteLine("    <HintPath>{0}</HintPath>", Helper.MakeFilePath(refr.Path, refr.Name, "exe"));
                    }
                    else if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "dll")))
                    {
                        // Assembly is an library (dll)
                        ps.WriteLine("    <HintPath>{0}</HintPath>", Helper.MakeFilePath(refr.Path, refr.Name, "dll"));
                    }
                    else
                    {
                        var referencePath = Helper.MakeFilePath(refr.Path, refr.Name, "dll");
                        kernel.Log.Write(LogType.Warning, "Reference \"{0}\": The specified file doesn't exist.",
                            referencePath);
                        ps.WriteLine("    <HintPath>{0}</HintPath>", Helper.MakeFilePath(refr.Path, refr.Name, "dll"));
                    }
                }

                ps.WriteLine("    <Private>{0}</Private>", refr.LocalCopy);
                ps.WriteLine("    </Reference>");
            }

            ps.WriteLine("  </ItemGroup>");
            ps.WriteLine();
        }

        //Project References
        if (projectReferences.Count > 0)
        {
            ps.WriteLine("  <ItemGroup>");
            foreach (var pair in projectReferences)
            {
                var tool = tools[pair.Value.Language];
                if (tools == null)
                    throw new UnknownLanguageException();

                var path =
                    Helper.MakePathRelativeTo(project.FullPath,
                        Helper.MakeFilePath(pair.Value.FullPath, pair.Value.Name, tool.FileExtension));
                ps.WriteLine("    <ProjectReference Include=\"{0}\">", path);

                // TODO: Allow reference to visual basic projects
                ps.WriteLine("    <Name>{0}</Name>", pair.Value.Name);
                ps.WriteLine("    <Project>{0}</Project>", pair.Value.Guid.ToString("B").ToUpper());
                ps.WriteLine("    <Package>{0}</Package>", tool.Guid.ToUpper());

                //This is the Copy Local flag in VS
                ps.WriteLine("    <Private>{0}</Private>", pair.Key.LocalCopy);

                ps.WriteLine("    </ProjectReference>");
            }

            ps.WriteLine("  </ItemGroup>");
            ps.WriteLine();
        }
    }

    private void WriteTextGeneratorNodes(ProjectNode project, StreamWriter ps)
    {
        foreach(TextGenNode node in project.TextGenNodes)
        {
            ps.WriteLine("  <Target Name=\"Prebuild\" BeforeTargets=\"PreBuildEvent\">");
            ps.WriteLine($"    <Exec Command=\"'dotnet' '$(DevEnvDir)TextTransformCore.dll' '$(ProjectDir){project.Path}\\{node.Name}'\" />");
            ps.WriteLine($"  </Target>");
        }
    }

    private void WriteProjectDotNet(SolutionNode solution, ProjectNode project, StreamWriter ps)
    {
        #region Project File

        var listFiles = project.Files.Count > 0;
        var prebuild = string.Empty;
        var postbuild = string.Empty;
        using (ps)
        {
            ps.WriteLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            ps.WriteLine();

            ps.WriteLine("  <PropertyGroup>");
            ps.WriteLine("    <TargetFramework>{0}</TargetFramework>",
                project.FrameworkVersion.ToString().Replace("_", "."));
            ps.WriteLine("    <PreserveCompilationContext>false</PreserveCompilationContext>");
            ps.WriteLine("    <OutputType>{0}</OutputType>",
                project.Type == ProjectType.Web ? ProjectType.Library.ToString() : project.Type.ToString());
            ps.WriteLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
            if (project.FrameworkVersion == FrameworkVersion.netstandard2_0)
                ps.WriteLine("    <RuntimeFrameworkVersion>5.0.0</RuntimeFrameworkVersion>");
            ps.WriteLine("    <ImplicitUsings>disable</ImplicitUsings>");
            ps.WriteLine("    <AssemblyName>{0}</AssemblyName>", project.AssemblyName);
            ps.WriteLine("    <Deterministic>true</Deterministic>");
            //ps.WriteLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
            ps.WriteLine("    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>");
            if (!solution.Options.UseDepsFile)
                ps.WriteLine("    <GenerateDependencyFile>false</GenerateDependencyFile>");
            if (listFiles)
                ps.WriteLine("    <EnableDefaultItems>false</EnableDefaultItems>");

            if (project.CopyLocalLockFileAssemblies)
                ps.WriteLine(
                    $"    <CopyLocalLockFileAssemblies>{project.CopyLocalLockFileAssemblies}</CopyLocalLockFileAssemblies>");

            ps.WriteLine($"<SelfContained>{solution.Options.SelfContained}</SelfContained>");
            if (solution.Options.UseRuntimeIdentifier)
            {
                var RID = "unknown";

                if (OperatingSystem.IsLinux()) RID = "linux-x64";

                if (OperatingSystem.IsWindows()) RID = "win-x64";

                if (OperatingSystem.IsMacOS()) RID = "macos-x64";

                ps.WriteLine($"<RuntimeIdentifier>{RID}</RuntimeIdentifier>");
            }

            ps.WriteLine("  </PropertyGroup>");
            ps.WriteLine();

            foreach (var conf in project.Configurations)
                if (conf.Name == "unknown")
                {
                    ps.WriteLine("  <PropertyGroup>");
                    foreach (var opt in conf.Options.AllDefined())
                        ps.WriteLine("    <{0}>{1}</{0}>", opt,
                            Helper.EndPath(Helper.NormalizePath(conf.Options[opt].ToString())));
                    ps.WriteLine("  </PropertyGroup>");
                    ps.WriteLine();
                }
                else
                {
                    ps.Write("  <PropertyGroup ");
                    ps.WriteLine("Condition=\" '$(Configuration)|$(Platform)' == '{0}|{1}' \">", conf.Name,
                        conf.Platform);
                    ps.WriteLine("    <AllowUnsafeBlocks>{0}</AllowUnsafeBlocks>", conf.Options["AllowUnsafe"]);
                    ps.WriteLine("    <BaseAddress>{0}</BaseAddress>", conf.Options["BaseAddress"]);
                    if ((bool)conf.Options["CheckUnderflowOverflow"])
                        ps.WriteLine("    <CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>");
                    ps.WriteLine("    <ConfigurationOverrideFile>");
                    ps.WriteLine("    </ConfigurationOverrideFile>");
                    if (string.IsNullOrEmpty(conf.Options["CompilerDefines"].ToString()))
                    {
                        ps.WriteLine("    <DefineConstants>{0}</DefineConstants>", kernel.ForcedConditionals);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(kernel.ForcedConditionals))
                            ps.WriteLine("    <DefineConstants>{0}</DefineConstants>", conf.Options["CompilerDefines"]);
                        else
                            ps.WriteLine("    <DefineConstants>{0}</DefineConstants>",
                                conf.Options["CompilerDefines"] + ";" + kernel.ForcedConditionals);
                    }

                    ps.WriteLine("    <DocumentationFile>{0}</DocumentationFile>",
                        Helper.NormalizePath(conf.Options["XmlDocFile"].ToString()));
                    ps.WriteLine("    <DebugSymbols>{0}</DebugSymbols>", conf.Options["DebugInformation"]);
                    ps.WriteLine("    <FileAlignment>{0}</FileAlignment>", conf.Options["FileAlignment"]);
                    ps.WriteLine("    <Optimize>{0}</Optimize>", conf.Options["OptimizeCode"]);
                    //ps.WriteLine("    <TieredCompilation>false</TieredCompilation>");
                    ps.WriteLine("    <TieredCompilationQuickJit>false</TieredCompilationQuickJit>");
                    ps.WriteLine("    <UseCommonOutputDirectory>{0}</UseCommonOutputDirectory>",
                        conf.Options["UseCommonOutputDirectory"]);
                    ps.WriteLine("    <AppendTargetFrameworkToOutputPath>{0}</AppendTargetFrameworkToOutputPath>",
                        conf.Options["AppendTargetFrameworkToOutputPath"]);
                    ps.WriteLine("    <AppendRuntimeIdentifierToOutputPath>{0}</AppendRuntimeIdentifierToOutputPath>",
                        conf.Options["AppendRuntimeIdentifierToOutputPath"]);

                    if (!listFiles && (bool)conf.Options["EnableDefaultItems"] == false)
                        ps.WriteLine("    <EnableDefaultItems>false</EnableDefaultItems>");

                    if (project.Type != ProjectType.Web)
                        ps.WriteLine("    <OutputPath>{0}</OutputPath>",
                            Helper.EndPath(Helper.NormalizePath(conf.Options["OutputPath"].ToString())));
                    else
                        ps.WriteLine("    <OutputPath>{0}</OutputPath>",
                            Helper.EndPath(Helper.NormalizePath("bin\\")));

                    ps.WriteLine("    <RegisterForComInterop>{0}</RegisterForComInterop>",
                        conf.Options["RegisterComInterop"]);
                    ps.WriteLine("    <RemoveIntegerChecks>{0}</RemoveIntegerChecks>",
                        conf.Options["RemoveIntegerChecks"]);
                    ps.WriteLine("    <TreatWarningsAsErrors>{0}</TreatWarningsAsErrors>",
                        conf.Options["WarningsAsErrors"]);
                    ps.WriteLine("    <WarningLevel>{0}</WarningLevel>", conf.Options["WarningLevel"]);
                    ps.WriteLine("    <NoStdLib>{0}</NoStdLib>", conf.Options["NoStdLib"]);
                    ps.WriteLine("    <NoWarn>{0}</NoWarn>", conf.Options["SuppressWarnings"]);
                    ps.WriteLine("    <PlatformTarget>{0}</PlatformTarget>", conf.Platform);

                    if (conf.Options["PreBuildEvent"] != null && conf.Options["PreBuildEvent"].ToString().Length != 0)
                        prebuild = conf.Options["PreBuildEvent"].ToString();
                    if (conf.Options["PostBuildEvent"] != null && conf.Options["PostBuildEvent"].ToString().Length != 0)
                        postbuild = conf.Options["PostBuildEvent"].ToString();

                    ps.WriteLine("  </PropertyGroup>");
                }

            if (project.ProjectReferences.Count > 0)
            {
                ps.WriteLine("  <ItemGroup>");
                foreach (var refer in project.ProjectReferences)
                    ps.WriteLine("    <ProjectReference Include=\"{0}\" />", refer.Include);
                ps.WriteLine("  </ItemGroup>");
                ps.WriteLine();
            }

            if (project.PackageReferences.Count > 0)
            {
                ps.WriteLine("  <ItemGroup>");
                foreach (var pack in project.PackageReferences)
                    ps.WriteLine("    <PackageReference Include=\"{0}\" Version=\"{1}\" />",
                        pack.Name, pack.Version);
                ps.WriteLine("  </ItemGroup>");
                ps.WriteLine();
            }

            // Output the ItemGroup for project.References
            WriteProjectReferencesDotNet(solution, project, ps);

            WriteTextGeneratorNodes(project, ps);

            if (project.Files.Count > 0)
            {
                var list = new List<string>();
                ps.WriteLine("  <ItemGroup>");
                foreach (var filePath in project.Files)
                {
                    // Add the filePath with the destination as the key
                    // will use it later to form the copy parameters with Include lists
                    // for each destination
                    if (project.Files.GetBuildAction(filePath) == BuildAction.Copy)
                        continue;
                    //if (file == "Properties\\Bind.Designer.cs")
                    //{
                    //    Console.WriteLine("Wait a minute!");
                    //    Console.WriteLine(project.Files.GetSubType(file).ToString());
                    //}
                    var subType = project.Files.GetSubType(filePath);

                    // Visual Studio chokes on file names if forward slash is used as a path separator
                    // instead of backslash.  So we must make sure that all file paths written to the
                    // project file use \ as a path separator.
                    var file = filePath.Replace(@"/", @"\");

                    if (subType != SubType.Code && subType != SubType.Settings && subType != SubType.Designer
                        && subType != SubType.CodeBehind)
                    {
                        ps.WriteLine("    <EmbeddedResource Include=\"{0}\">",
                            file.Substring(0, file.LastIndexOf('.')) + ".resx");
                        ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(file));
                        ps.WriteLine("      <SubType>Designer</SubType>");
                        ps.WriteLine("    </EmbeddedResource>");
                        //
                    }

                    if (subType == SubType.Designer)
                    {
                        ps.WriteLine("    <EmbeddedResource Include=\"{0}\">", file);

                        var autogen_name = file.Substring(0, file.LastIndexOf('.')) + ".Designer.cs";
                        var dependent_name = filePath.Substring(0, file.LastIndexOf('.')) + ".cs";

                        // Check for a parent .cs file with the same name as this designer file
                        if (File.Exists(Helper.NormalizePath(dependent_name)))
                        {
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(dependent_name));
                        }
                        else
                        {
                            ps.WriteLine("      <Generator>ResXFileCodeGenerator</Generator>");
                            ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>", Path.GetFileName(autogen_name));
                            ps.WriteLine("      <SubType>" + subType + "</SubType>");
                        }

                        ps.WriteLine("    </EmbeddedResource>");
                        if (File.Exists(Helper.NormalizePath(autogen_name)))
                        {
                            ps.WriteLine("    <Compile Include=\"{0}\">", autogen_name);
                            //ps.WriteLine("	  <DesignTime>True</DesignTime>");

                            // If a parent .cs file exists, link this autogen file to it. Otherwise link
                            // to the designer file
                            if (File.Exists(dependent_name))
                            {
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                    Path.GetFileName(dependent_name));
                            }
                            else
                            {
                                ps.WriteLine("      <AutoGen>True</AutoGen>");
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", Path.GetFileName(filePath));
                            }

                            ps.WriteLine("    </Compile>");
                        }

                        list.Add(autogen_name);
                    }

                    if (subType == SubType.Settings)
                    {
                        ps.Write("    <{0} ", project.Files.GetBuildAction(filePath));
                        ps.WriteLine("Include=\"{0}\">", file);
                        var fileName = Path.GetFileName(filePath);
                        if (project.Files.GetBuildAction(filePath) == BuildAction.None)
                        {
                            ps.WriteLine("      <Generator>SettingsSingleFileGenerator</Generator>");
                            ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>",
                                fileName.Substring(0, fileName.LastIndexOf('.')) + ".Designer.cs");
                        }
                        else
                        {
                            ps.WriteLine("      <SubType>Code</SubType>");
                            ps.WriteLine("      <AutoGen>True</AutoGen>");
                            ps.WriteLine("      <DesignTimeSharedInput>True</DesignTimeSharedInput>");
                            var fileNameShort = fileName.Substring(0, fileName.LastIndexOf('.'));
                            var fileNameShorter = fileNameShort.Substring(0, fileNameShort.LastIndexOf('.'));
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                Path.GetFileName(fileNameShorter + ".settings"));
                        }

                        ps.WriteLine("    </{0}>", project.Files.GetBuildAction(filePath));
                    }
                    else if (subType != SubType.Designer)
                    {
                        var path = Helper.NormalizePath(file);
                        var path_lower = path.ToLower();

                        if (!list.Contains(filePath))
                        {
                            ps.Write("    <{0} ", project.Files.GetBuildAction(filePath));

                            var startPos = 0;
                            if (project.Files.GetPreservePath(filePath))
                                while (@"./\".IndexOf(file.Substring(startPos, 1)) != -1)
                                    startPos++;
                            else
                                startPos = file.LastIndexOf(Path.GetFileName(path));

                            // be sure to write out the path with backslashes so VS recognizes
                            // the file properly.
                            ps.WriteLine("Include=\"{0}\">", file);

                            var last_period_index = file.LastIndexOf('.');
                            var short_file_name = last_period_index >= 0
                                ? file.Substring(0, last_period_index)
                                : file;
                            var extension = Path.GetExtension(path);
                            // make this upper case, so that when File.Exists tests for the
                            // existence of a designer file on a case-sensitive platform,
                            // it is correctly identified.
                            var designer_format = string.Format(".Designer{0}", extension);

                            if (path_lower.EndsWith(designer_format.ToLowerInvariant()))
                            {
                                var designer_index = path.IndexOf(designer_format);
                                var file_name = path.Substring(0, designer_index);

                                // There are two corrections to the next lines:
                                // 1. Fix the connection between a designer file and a form
                                //	  or usercontrol that don't have an associated resx file.
                                // 2. Connect settings files to associated designer files.
                                if (File.Exists(file_name + extension))
                                {
                                    ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                        Path.GetFileName(file_name + extension));
                                }
                                else if (File.Exists(file_name + ".resx"))
                                {
                                    ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                        Path.GetFileName(file_name + ".resx"));
                                }
                                else if (File.Exists(file_name + ".settings"))
                                {
                                    ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                        Path.GetFileName(file_name + ".settings"));
                                    ps.WriteLine("      <AutoGen>True</AutoGen>");
                                    ps.WriteLine("      <DesignTimeSharedInput>True</DesignTimeSharedInput>");
                                }
                            }
                            else if (subType == SubType.CodeBehind)
                            {
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>",
                                    Path.GetFileName(short_file_name));
                            }

                            if (project.Files.GetIsLink(filePath))
                            {
                                var alias = project.Files.GetLinkPath(filePath);
                                alias += file.Substring(startPos);
                                alias = Helper.NormalizePath(alias);
                                ps.WriteLine("      <Link>{0}</Link>", alias);
                            }
                            else if (project.Files.GetBuildAction(filePath) != BuildAction.None)
                            {
                                if (project.Files.GetBuildAction(filePath) != BuildAction.EmbeddedResource)
                                    ps.WriteLine("      <SubType>{0}</SubType>", subType);
                            }

                            if (project.Files.GetCopyToOutput(filePath) != CopyToOutput.Never)
                                ps.WriteLine("      <CopyToOutputDirectory>{0}</CopyToOutputDirectory>",
                                    project.Files.GetCopyToOutput(filePath));

                            ps.WriteLine("    </{0}>", project.Files.GetBuildAction(filePath));
                        }
                    }
                }

                ps.WriteLine("  </ItemGroup>");
            }

            if (!string.IsNullOrEmpty(prebuild))
            {
                ps.WriteLine("    <Target Name=\"PreBuild\" BeforeTargets=\"PreBuildEvent\">");
                ps.WriteLine("      <Exec Command = \"{0}\" />", prebuild);
                ps.WriteLine("    </Target>");
            }

            if (!string.IsNullOrEmpty(postbuild))
            {
                ps.WriteLine("    <Target Name = \"PostBuild\" AfterTargets = \"PostBuildEvent\">");
                ps.WriteLine("      <Exec Command = \"{0}\" />", postbuild);
                ps.WriteLine("    </Target>");
            }

            ps.WriteLine("</Project>");

        }

        #endregion
    }

    private void WriteProjectReferencesDotNet(SolutionNode solution, ProjectNode project, StreamWriter ps)
    {
        var projectReferences = new Dictionary<ReferenceNode, ProjectNode>();
        var otherReferences = new List<ReferenceNode>();

        foreach (var refr in project.References)
        {
            var projectNode = FindProjectInSolution(refr.Name, solution);

            if (projectNode == null)
                otherReferences.Add(refr);
            else
                projectReferences.Add(refr, projectNode);
        }

        // Assembly References
        if (otherReferences.Count > 0)
        {
            ps.WriteLine("  <ItemGroup>");

            foreach (var refr in otherReferences)
            {
                ps.Write("    <Reference");
                //ps.Write(" Update=\"");
                ps.Write(" Include=\"");
                ps.Write(refr.Name);
                ps.WriteLine("\" >");
                ps.Write("      <Name>");
                ps.Write(refr.Name);
                ps.WriteLine("</Name>");

                if (!string.IsNullOrEmpty(refr.Path))
                {
                    // Use absolute path to assembly (for determining assembly type)
                    var absolutePath = Path.Combine(project.FullPath, refr.Path);
                    if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "exe")))
                    {
                        // Assembly is an executable (exe)
                        ps.WriteLine("      <HintPath>{0}</HintPath>",
                            Helper.MakeFilePath(refr.Path, refr.Name, "exe"));
                    }
                    else if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "dll")))
                    {
                        // Assembly is an library (dll)
                        ps.WriteLine("      <HintPath>{0}</HintPath>",
                            Helper.MakeFilePath(refr.Path, refr.Name, "dll"));
                    }
                    else
                    {
                        var referencePath = Helper.MakeFilePath(refr.Path, refr.Name, "dll");
                        kernel.Log.Write(LogType.Warning, "Reference \"{0}\": The specified file doesn't exist.",
                            referencePath);
                        ps.WriteLine("      <HintPath>{0}</HintPath>",
                            Helper.MakeFilePath(refr.Path, refr.Name, "dll"));
                    }
                }
                else
                {
                    // this may be wrong
                    // Use absolute path to assembly (for determining assembly type)
                    var absolutePath = Path.Combine(project.FullPath, MakeRefPath(project));
                    if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "exe")))
                        // Assembly is an executable (exe)
                        ps.WriteLine("      <HintPath>{0}</HintPath>",
                            Helper.MakeFilePath(absolutePath, refr.Name, "exe"));
                    else if (File.Exists(Helper.MakeFilePath(absolutePath, refr.Name, "dll")))
                        // Assembly is an library (dll)
                        ps.WriteLine("      <HintPath>{0}</HintPath>",
                            Helper.MakeFilePath(absolutePath, refr.Name, "dll"));
                    // string referencePath = Helper.MakeFilePath(refr.Path, refr.Name, "dll");
                    // kernel.Log.Write(LogType.Warning, "Reference \"{0}\": The specified file doesn't exist.", referencePath);
                    // ps.WriteLine("		<HintPath>{0}</HintPath>", Helper.MakeFilePath(absolutePath, refr.Name, "dll"));
                }

                ps.WriteLine("      <Private>{0}</Private>", refr.LocalCopy);
                ps.WriteLine("    </Reference>");
            }

            ps.WriteLine("  </ItemGroup>");
            ps.WriteLine();
        }

        //Project References
        if (projectReferences.Count > 0)
        {
            ps.WriteLine("  <ItemGroup>");
            foreach (var pair in projectReferences)
            {
                var tool = tools[pair.Value.Language];
                if (tools == null)
                    throw new UnknownLanguageException();

                var path =
                    Helper.MakePathRelativeTo(project.FullPath,
                        Helper.MakeFilePath(pair.Value.FullPath, pair.Value.Name, tool.FileExtension));
                ps.WriteLine("    <ProjectReference Include=\"{0}\" />", path);
            }

            ps.WriteLine("  </ItemGroup>");
            ps.WriteLine();
        }
    }

    private void WriteSolution(SolutionNode solution, bool writeSolutionToDisk)
    {
        kernel.Log.Write("Creating {0} solution and project files", VersionName);

        foreach (var child in solution.Solutions)
        {
            kernel.Log.Write("...Creating folder: {0}", child.Name);
            WriteSolution(child, false);
        }

        foreach (var project in solution.Projects)
        {
            kernel.Log.Write("...Creating project: {0}", project.Name);
            WriteProject(solution, project);
        }

        foreach (var project in solution.DatabaseProjects)
        {
            kernel.Log.Write("...Creating database project: {0}", project.Name);
            WriteDatabaseProject(solution, project);
        }

        if (writeSolutionToDisk) // only write main solution
        {
            kernel.Log.Write("");
            var solutionFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");

            using (var ss = new StreamWriter(solutionFile))
            {
                kernel.CurrentWorkingDirectory.Push();
                Helper.SetCurrentDir(Path.GetDirectoryName(solutionFile));

                ss.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", SolutionVersion);
                ss.WriteLine(SolutionTag);

                WriteProjectDeclarations(ss, solution, solution);

                ss.WriteLine("Global");

                ss.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (var conf in solution.Configurations) ss.WriteLine("\t\t{0} = {0}", conf.NameAndPlatform);
                ss.WriteLine("\tEndGlobalSection");

                ss.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
                WriteConfigurationLines(solution.Configurations, solution, ss);
                ss.WriteLine("\tEndGlobalSection");

                if (solution.Solutions.Count > 0)
                {
                    ss.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                    foreach (var embeddedSolution in solution.Solutions) WriteNestedProjectMap(ss, embeddedSolution);
                    ss.WriteLine("\tEndGlobalSection");
                }

                ss.WriteLine("EndGlobal");
            }

            kernel.CurrentWorkingDirectory.Pop();
        }
    }

    private void WriteProjectDeclarations(TextWriter writer, SolutionNode actualSolution, SolutionNode embeddedSolution)
    {
        foreach (var childSolution in embeddedSolution.Solutions)
        {
            WriteEmbeddedSolution(writer, childSolution);
            WriteProjectDeclarations(writer, actualSolution, childSolution);
        }

        foreach (var project in embeddedSolution.Projects) WriteProject(actualSolution, writer, project);

        foreach (var dbProject in embeddedSolution.DatabaseProjects) WriteProject(actualSolution, writer, dbProject);

        if (actualSolution.Guid == embeddedSolution.Guid) WriteSolutionFiles(actualSolution, writer);
    }

    private static void WriteNestedProjectMap(TextWriter writer, SolutionNode embeddedSolution)
    {
        foreach (var project in embeddedSolution.Projects) WriteNestedProject(writer, embeddedSolution, project.Guid);

        foreach (var dbProject in embeddedSolution.DatabaseProjects)
            WriteNestedProject(writer, embeddedSolution, dbProject.Guid);

        foreach (var child in embeddedSolution.Solutions)
        {
            WriteNestedProject(writer, embeddedSolution, child.Guid);
            WriteNestedProjectMap(writer, child);
        }
    }

    private static void WriteNestedProject(TextWriter writer, SolutionNode solution, Guid projectGuid)
    {
        WriteNestedFolder(writer, solution.Guid, projectGuid);
    }

    private static void WriteNestedFolder(TextWriter writer, Guid parentGuid, Guid childGuid)
    {
        writer.WriteLine("\t\t{0} = {1}",
            childGuid.ToString("B").ToUpper(),
            parentGuid.ToString("B").ToUpper());
    }

    private static void WriteConfigurationLines(IEnumerable<ConfigurationNode> configurations, SolutionNode solution,
        TextWriter ss)
    {
        foreach (var project in solution.Projects)
        foreach (var conf in configurations)
        {
            ss.WriteLine("\t\t{0}.{1}.ActiveCfg = {1}",
                project.Guid.ToString("B").ToUpper(),
                conf.NameAndPlatform);

            ss.WriteLine("\t\t{0}.{1}.Build.0 = {1}",
                project.Guid.ToString("B").ToUpper(),
                conf.NameAndPlatform);
        }

        foreach (var child in solution.Solutions) WriteConfigurationLines(configurations, child, ss);
    }

    private void WriteSolutionFiles(SolutionNode solution, TextWriter ss)
    {
        if (solution.Files != null && solution.Files.Count > 0)
            WriteProject(ss, "Folder", solution.Guid, "Solution Files", "Solution Files", solution.Files);
    }

    private void WriteEmbeddedSolution(TextWriter writer, SolutionNode embeddedSolution)
    {
        WriteProject(writer, "Folder", embeddedSolution.Guid, embeddedSolution.Name, embeddedSolution.Name,
            embeddedSolution.Files);
    }

    private void WriteProject(SolutionNode solution, TextWriter ss, ProjectNode project)
    {
        WriteProject(ss, solution, project.Language, project.Guid, project.Name, project.FullPath);
    }

    private void WriteProject(SolutionNode solution, TextWriter ss, DatabaseProjectNode dbProject)
    {
        if (solution.Files != null && solution.Files.Count > 0)
            WriteProject(ss, solution, "Database", dbProject.Guid, dbProject.Name, dbProject.FullPath);
    }

    private const string ProjectDeclarationBeginFormat = "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"";
    private const string ProjectDeclarationEndFormat = "EndProject";

    private void WriteProject(TextWriter ss, SolutionNode solution, string language, Guid guid, string name,
        string projectFullPath)
    {
        if (!tools.ContainsKey(language))
            throw new UnknownLanguageException("Unknown .NET language: " + language);

        var toolInfo = tools[language];

        var path = Helper.MakePathRelativeTo(solution.FullPath, projectFullPath);

        path = Helper.MakeFilePath(path, name, toolInfo.FileExtension);

        WriteProject(ss, language, guid, name, path);
    }

    private void WriteProject(TextWriter writer, string language, Guid projectGuid, string name, string location)
    {
        WriteProject(writer, language, projectGuid, name, location, null);
    }

    private void WriteProject(TextWriter writer, string language, Guid projectGuid, string name, string location,
        FilesNode files)
    {
        if (!tools.ContainsKey(language))
            throw new UnknownLanguageException("Unknown .NET language: " + language);

        var toolInfo = tools[language];

        writer.WriteLine(ProjectDeclarationBeginFormat,
            toolInfo.Guid,
            name,
            location,
            projectGuid.ToString("B").ToUpper());

        if (files != null)
        {
            writer.WriteLine("\tProjectSection(SolutionItems) = preProject");

            foreach (var file in files)
                writer.WriteLine("\t\t{0} = {0}", file);

            writer.WriteLine("\tEndProjectSection");
        }

        writer.WriteLine(ProjectDeclarationEndFormat);
    }

    private void WriteDatabaseProject(SolutionNode solution, DatabaseProjectNode project)
    {
        var projectFile = Helper.MakeFilePath(project.FullPath, project.Name, "dbp");
        var ps = new IndentedTextWriter(new StreamWriter(projectFile), "	  ");

        kernel.CurrentWorkingDirectory.Push();

        Helper.SetCurrentDir(Path.GetDirectoryName(projectFile));

        using (ps)
        {
            ps.WriteLine("# Microsoft Developer Studio Project File - Database Project");
            ps.WriteLine("Begin DataProject = \"{0}\"", project.Name);
            ps.Indent++;
            ps.WriteLine("MSDTVersion = \"80\"");
            // TODO: Use the project.Files property
            if (ContainsSqlFiles(Path.GetDirectoryName(projectFile)))
                WriteDatabaseFoldersAndFiles(ps, Path.GetDirectoryName(projectFile));

            ps.WriteLine("Begin DBRefFolder = \"Database References\"");
            ps.Indent++;
            foreach (var reference in project.References)
            {
                ps.WriteLine("Begin DBRefNode = \"{0}\"", reference.Name);
                ps.Indent++;
                ps.WriteLine("ConnectStr = \"{0}\"", reference.ConnectionString);
                ps.WriteLine("Provider = \"{0}\"", reference.ProviderId.ToString("B").ToUpper());
                //ps.WriteLine("Colorizer = 5");
                ps.Indent--;
                ps.WriteLine("End");
            }

            ps.Indent--;
            ps.WriteLine("End");
            ps.Indent--;
            ps.WriteLine("End");

            ps.Flush();
        }

        kernel.CurrentWorkingDirectory.Pop();
    }

    private static bool ContainsSqlFiles(string folder)
    {
        if (Directory.GetFiles(folder, "*.sql").Length > 0)
            return true; // if the folder contains 1 .sql file, that's good enough

        foreach (var child in Directory.GetDirectories(folder))
            if (ContainsSqlFiles(child))
                return true; // if 1 child folder contains a .sql file, still good enough

        return false;
    }

    private static void WriteDatabaseFoldersAndFiles(IndentedTextWriter writer, string folder)
    {
        foreach (var child in Directory.GetDirectories(folder))
            if (ContainsSqlFiles(child))
            {
                writer.WriteLine("Begin Folder = \"{0}\"", Path.GetFileName(child));
                writer.Indent++;
                WriteDatabaseFoldersAndFiles(writer, child);
                writer.Indent--;
                writer.WriteLine("End");
            }

        foreach (var file in Directory.GetFiles(folder, "*.sql"))
            writer.WriteLine("Script = \"{0}\"", Path.GetFileName(file));
    }

    private void CleanProject(ProjectNode project)
    {
        kernel.Log.Write("...Cleaning project: {0}", project.Name);

        var toolInfo = tools[project.Language];
        var projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
        var userFile = projectFile + ".user";

        Helper.DeleteIfExists(projectFile);
        Helper.DeleteIfExists(userFile);

        var projectobj = Helper.MakeFilePath(project.FullPath, "obj");
        Helper.DeleteFolderIfExists(projectobj);
    }

    private void CleanSolution(SolutionNode solution)
    {
        kernel.Log.Write("Cleaning {0} solution and project files", VersionName, solution.Name);

        var slnFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
        var suoFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "suo");

        Helper.DeleteIfExists(slnFile);
        Helper.DeleteIfExists(suoFile);

        foreach (var project in solution.Projects) CleanProject(project);

        kernel.Log.Write("");
    }

    #endregion

    #region ITarget Members

    /// <summary>
    ///     Writes the specified kern.
    /// </summary>
    /// <param name="kern">The kern.</param>
    public virtual void Write(Kernel kern)
    {
        if (kern == null) throw new ArgumentNullException("kern");
        kernel = kern;
        foreach (var sol in kernel.Solutions) WriteSolution(sol, true);
        kernel = null;
    }

    /// <summary>
    ///     Cleans the specified kern.
    /// </summary>
    /// <param name="kern">The kern.</param>
    public virtual void Clean(Kernel kern)
    {
        if (kern == null) throw new ArgumentNullException("kern");
        kernel = kern;
        foreach (var sol in kernel.Solutions) CleanSolution(sol);
        kernel = null;
    }

    #endregion
}