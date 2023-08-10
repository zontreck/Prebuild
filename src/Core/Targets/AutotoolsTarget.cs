#region BSD License

/*

Copyright (c) 2004 - 2008
Matthew Holmes        (matthew@wildfiregames.com),
Dan     Moorehead     (dan05a@gmail.com),
Dave    Hudson        (jendave@yahoo.com),
C.J.    Adams-Collier (cjac@colliertech.org),

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

* Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.

* The name of the author may not be used to endorse or promote
products derived from this software without specific prior written
permission.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.

*/

#endregion

#region MIT X11 license

/*
 Portions of this file authored by Lluis Sanchez Gual

 Copyright (C) 2006 Novell, Inc (http://www.novell.com)

 Permission is hereby granted, free of charge, to any person obtaining
 a copy of this software and associated documentation files (the
 "Software"), to deal in the Software without restriction, including
 without limitation the rights to use, copy, modify, merge, publish,
 distribute, sublicense, and/or sell copies of the Software, and to
 permit persons to whom the Software is furnished to do so, subject to
 the following conditions:
 
 The above copyright notice and this permission notice shall be
 included in all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Parse;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Targets;

public enum ClrVersion
{
    Default,
    Net_1_1,
    Net_2_0
}

public class SystemPackage
{
    public string Name { get; private set; }

    public string Version { get; private set; }

    public string Description { get; private set; }

    public ClrVersion TargetVersion { get; private set; }

    // The package is part of the mono SDK
    public bool IsCorePackage => Name == "mono";

    // The package has been registered by an add-in, and is not installed
    // in the system.
    public bool IsInternalPackage { get; private set; }

    public string[] Assemblies { get; private set; }

    public void Initialize(string name,
        string version,
        string description,
        string[] assemblies,
        ClrVersion targetVersion,
        bool isInternal)
    {
        IsInternalPackage = isInternal;
        Name = name;
        Version = version;
        Assemblies = assemblies;
        Description = description;
        TargetVersion = targetVersion;
    }
}

/// <summary>
/// </summary>
[Target("autotools")]
public class AutotoolsTarget : ITarget
{
    #region Fields

    private Kernel m_Kernel;
    private XmlDocument autotoolsDoc;
    private XmlUrlResolver xr;
    private readonly Dictionary<string, SystemPackage> assemblyPathToPackage = new();
    private readonly Dictionary<string, string> assemblyFullNameToPath = new();
    private readonly Dictionary<string, SystemPackage> packagesHash = new();
    private readonly List<SystemPackage> packages = new();

    #endregion

    #region Private Methods

    private static void mkdirDashP(string dirName)
    {
        var di = new DirectoryInfo(dirName);
        if (di.Exists)
            return;

        var parentDirName = Path.GetDirectoryName(dirName);
        var parentDi = new DirectoryInfo(parentDirName);
        if (!parentDi.Exists)
            mkdirDashP(parentDirName);

        di.Create();
    }

    private static void chkMkDir(string dirName)
    {
        var di =
            new DirectoryInfo(dirName);

        if (!di.Exists)
            di.Create();
    }

    private void transformToFile(string filename, XsltArgumentList argList, string nodeName)
    {
        // Create an XslTransform for this file
        var templateTransformer = new XslTransform();

        // Load up the template
        var templateNode = autotoolsDoc.SelectSingleNode(nodeName + "/*");
        //templateTransformer.Load(templateNode.CreateNavigator(), xr, e);
        templateTransformer.Load(templateNode.CreateNavigator(), xr);
        // Create a writer for the transformed template
        var templateWriter = new XmlTextWriter(filename, null);

        // Perform transformation, writing the file
        templateTransformer.Transform(m_Kernel.CurrentDoc, argList, templateWriter, xr);
    }

    private static string NormalizeAsmName(string name)
    {
        var i = name.IndexOf(", PublicKeyToken=null");
        if (i != -1)
            return name.Substring(0, i).Trim();
        return name;
    }

    private void AddAssembly(string assemblyfile, SystemPackage package)
    {
        if (!File.Exists(assemblyfile))
            return;

        try
        {
            var an = AssemblyName.GetAssemblyName(assemblyfile);
            assemblyFullNameToPath[NormalizeAsmName(an.FullName)] = assemblyfile;
            assemblyPathToPackage[assemblyfile] = package;
        }
        catch
        {
        }
    }

    private static List<string> GetAssembliesWithLibInfo(string line, string file)
    {
        var references = new List<string>();
        var libdirs = new List<string>();
        var retval = new List<string>();
        foreach (var piece in line.Split(' '))
            if (piece.ToLower().Trim().StartsWith("/r:") || piece.ToLower().Trim().StartsWith("-r:"))
                references.Add(ProcessPiece(piece.Substring(3).Trim(), file));
            else if (piece.ToLower().Trim().StartsWith("/lib:") || piece.ToLower().Trim().StartsWith("-lib:"))
                libdirs.Add(ProcessPiece(piece.Substring(5).Trim(), file));

        foreach (var refrnc in references)
        foreach (var libdir in libdirs)
            if (File.Exists(libdir + Path.DirectorySeparatorChar + refrnc))
                retval.Add(libdir + Path.DirectorySeparatorChar + refrnc);

        return retval;
    }

    private static List<string> GetAssembliesWithoutLibInfo(string line, string file)
    {
        var references = new List<string>();
        foreach (var reference in line.Split(' '))
            if (reference.ToLower().Trim().StartsWith("/r:") || reference.ToLower().Trim().StartsWith("-r:"))
            {
                var final_ref = reference.Substring(3).Trim();
                references.Add(ProcessPiece(final_ref, file));
            }

        return references;
    }

    private static string ProcessPiece(string piece, string pcfile)
    {
        var start = piece.IndexOf("${");
        if (start == -1)
            return piece;

        var end = piece.IndexOf("}");
        if (end == -1)
            return piece;

        var variable = piece.Substring(start + 2, end - start - 2);
        var interp = GetVariableFromPkgConfig(variable, Path.GetFileNameWithoutExtension(pcfile));
        return ProcessPiece(piece.Replace("${" + variable + "}", interp), pcfile);
    }

    private static string GetVariableFromPkgConfig(string var, string pcfile)
    {
        var psi = new ProcessStartInfo("pkg-config");
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.Arguments = string.Format("--variable={0} {1}", var, pcfile);
        var p = new Process();
        p.StartInfo = psi;
        p.Start();
        var ret = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        if (string.IsNullOrEmpty(ret))
            return string.Empty;
        return ret;
    }

    private void ParsePCFile(string pcfile)
    {
        // Don't register the package twice
        var pname = Path.GetFileNameWithoutExtension(pcfile);
        if (packagesHash.ContainsKey(pname))
            return;

        List<string> fullassemblies = null;
        var version = "";
        var desc = "";

        var package = new SystemPackage();

        using (var reader = new StreamReader(pcfile))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var lowerLine = line.ToLower();
                if (lowerLine.StartsWith("libs:") && lowerLine.IndexOf(".dll") != -1)
                {
                    var choppedLine = line.Substring(5).Trim();
                    if (choppedLine.IndexOf("-lib:") != -1 || choppedLine.IndexOf("/lib:") != -1)
                        fullassemblies = GetAssembliesWithLibInfo(choppedLine, pcfile);
                    else
                        fullassemblies = GetAssembliesWithoutLibInfo(choppedLine, pcfile);
                }
                else if (lowerLine.StartsWith("version:"))
                {
                    // "version:".Length == 8
                    version = line.Substring(8).Trim();
                }
                else if (lowerLine.StartsWith("description:"))
                {
                    // "description:".Length == 12
                    desc = line.Substring(12).Trim();
                }
            }
        }

        if (fullassemblies == null)
            return;

        foreach (var assembly in fullassemblies) AddAssembly(assembly, package);

        package.Initialize(pname,
            version,
            desc,
            fullassemblies.ToArray(),
            ClrVersion.Default,
            false);
        packages.Add(package);
        packagesHash[pname] = package;
    }

    private void RegisterSystemAssemblies(string prefix, string version, ClrVersion ver)
    {
        var package = new SystemPackage();
        var list = new List<string>();

        var dir = Path.Combine(prefix, version);
        if (!Directory.Exists(dir)) return;

        foreach (var assembly in Directory.GetFiles(dir, "*.dll"))
        {
            AddAssembly(assembly, package);
            list.Add(assembly);
        }

        package.Initialize("mono",
            version,
            "The Mono runtime",
            list.ToArray(),
            ver,
            false);
        packages.Add(package);
    }

    private void RunInitialization()
    {
        string versionDir;

        if (Environment.Version.Major == 1)
            versionDir = "1.0";
        else
            versionDir = "2.0";

        //Pull up assemblies from the installed mono system.
        var prefix = Path.GetDirectoryName(typeof(int).Assembly.Location);

        if (prefix.IndexOf(Path.Combine("mono", versionDir)) == -1)
            prefix = Path.Combine(prefix, "mono");
        else
            prefix = Path.GetDirectoryName(prefix);

        RegisterSystemAssemblies(prefix, "1.0", ClrVersion.Net_1_1);
        RegisterSystemAssemblies(prefix, "2.0", ClrVersion.Net_2_0);

        var search_dirs = Environment.GetEnvironmentVariable("PKG_CONFIG_PATH");
        var libpath = Environment.GetEnvironmentVariable("PKG_CONFIG_LIBPATH");

        if (string.IsNullOrEmpty(libpath))
        {
            var path_dirs = Environment.GetEnvironmentVariable("PATH");
            foreach (var pathdir in path_dirs.Split(Path.PathSeparator))
            {
                if (pathdir == null)
                    continue;
                if (File.Exists(pathdir + Path.DirectorySeparatorChar + "pkg-config"))
                {
                    libpath = Path.Combine(pathdir, "..");
                    libpath = Path.Combine(libpath, "lib");
                    libpath = Path.Combine(libpath, "pkgconfig");
                    break;
                }
            }
        }

        search_dirs += Path.PathSeparator + libpath;
        if (!string.IsNullOrEmpty(search_dirs))
        {
            var scanDirs = new List<string>();
            foreach (var potentialDir in search_dirs.Split(Path.PathSeparator))
                if (!scanDirs.Contains(potentialDir))
                    scanDirs.Add(potentialDir);
            foreach (var pcdir in scanDirs)
            {
                if (pcdir == null)
                    continue;

                if (Directory.Exists(pcdir))
                    foreach (var pcfile in Directory.GetFiles(pcdir, "*.pc"))
                        ParsePCFile(pcfile);
            }
        }
    }

    private void WriteCombine(SolutionNode solution)
    {
        #region "Create Solution directory if it doesn't exist"

        var solutionDir = Path.Combine(solution.FullPath,
            Path.Combine("autotools",
                solution.Name));
        chkMkDir(solutionDir);

        #endregion

        #region "Write Solution-level files"

        var argList = new XsltArgumentList();
        argList.AddParam("solutionName", "", solution.Name);
        // $solutionDir is $rootDir/$solutionName/
        transformToFile(Path.Combine(solutionDir, "configure.ac"),
            argList, "/Autotools/SolutionConfigureAc");
        transformToFile(Path.Combine(solutionDir, "Makefile.am"),
            argList, "/Autotools/SolutionMakefileAm");
        transformToFile(Path.Combine(solutionDir, "autogen.sh"),
            argList, "/Autotools/SolutionAutogenSh");

        #endregion

        foreach (var project in solution.ProjectsTableOrder)
        {
            m_Kernel.Log.Write(string.Format("Writing project: {0}",
                project.Name));
            WriteProject(solution, project);
        }
    }

    private static string FindFileReference(string refName,
        ProjectNode project)
    {
        foreach (var refPath in project.ReferencePaths)
        {
            var fullPath =
                Helper.MakeFilePath(refPath.Path, refName, "dll");

            if (File.Exists(fullPath)) return fullPath;
        }

        return null;
    }

    /// <summary>
    ///     Gets the XML doc file.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="conf">The conf.</param>
    /// <returns></returns>
    public static string GetXmlDocFile(ProjectNode project, ConfigurationNode conf)
    {
        if (conf == null) throw new ArgumentNullException("conf");
        if (project == null) throw new ArgumentNullException("project");
        var docFile = (string)conf.Options["XmlDocFile"];
        //			if(docFile != null && docFile.Length == 0)//default to assembly name if not specified
        //			{
        //				return Path.GetFileNameWithoutExtension(project.AssemblyName) + ".xml";
        //			}
        return docFile;
    }

    /// <summary>
    ///     Normalizes the path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static string NormalizePath(string path)
    {
        if (path == null) return "";

        StringBuilder tmpPath;

        if (Preprocessor.GetOS() == "Win32")
        {
            tmpPath = new StringBuilder(path.Replace('\\', '/'));
            tmpPath.Replace("/", @"\\");
        }
        else
        {
            tmpPath = new StringBuilder(path.Replace('\\', '/'));
            tmpPath = tmpPath.Replace('/', Path.DirectorySeparatorChar);
        }

        return tmpPath.ToString();
    }

    private void WriteProject(SolutionNode solution, ProjectNode project)
    {
        var solutionDir = Path.Combine(solution.FullPath, Path.Combine("autotools", solution.Name));
        var projectDir = Path.Combine(solutionDir, project.Name);
        var projectVersion = project.Version;
        var hasAssemblyConfig = false;
        chkMkDir(projectDir);

        List<string>
            compiledFiles = new(),
            contentFiles = new(),
            embeddedFiles = new(),
            binaryLibs = new(),
            pkgLibs = new(),
            systemLibs = new(),
            runtimeLibs = new(),
            extraDistFiles = new(),
            localCopyTargets = new();

        // If there exists a .config file for this assembly, copy
        // it to the project folder

        // TODO: Support copying .config.osx files
        // TODO: support processing the .config file for native library deps
        var projectAssemblyName = project.Name;
        if (project.AssemblyName != null)
            projectAssemblyName = project.AssemblyName;

        if (File.Exists(Path.Combine(project.FullPath, projectAssemblyName) + ".dll.config"))
        {
            hasAssemblyConfig = true;
            File.Copy(Path.Combine(project.FullPath, projectAssemblyName + ".dll.config"),
                Path.Combine(projectDir, projectAssemblyName + ".dll.config"), true);
            extraDistFiles.Add(project.AssemblyName + ".dll.config");
        }

        foreach (var conf in project.Configurations)
            if (conf.Options.KeyFile != string.Empty)
            {
                // Copy snk file into the project's directory
                // Use the snk from the project directory directly
                var source = Path.Combine(project.FullPath, conf.Options.KeyFile);
                var keyFile = conf.Options.KeyFile;
                var re = new Regex(".*/");
                keyFile = re.Replace(keyFile, "");

                var dest = Path.Combine(projectDir, keyFile);
                // Tell the user if there's a problem copying the file
                try
                {
                    mkdirDashP(Path.GetDirectoryName(dest));
                    File.Copy(source, dest, true);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        // Copy compiled, embedded and content files into the project's directory
        foreach (var filename in project.Files)
        {
            var source = Path.Combine(project.FullPath, filename);
            var dest = Path.Combine(projectDir, filename);

            /*
            if (filename.Contains("AssemblyInfo.cs"))
            {
                // If we've got an AssemblyInfo.cs, pull the version number from it
                string[] sources = { source };
                string[] args = { "" };
                Microsoft.CSharp.CSharpCodeProvider cscp =
                    new Microsoft.CSharp.CSharpCodeProvider();

                string tempAssemblyFile = Path.Combine(Path.GetTempPath(), project.Name + "-temp.dll");
                System.CodeDom.Compiler.CompilerParameters cparam =
                    new System.CodeDom.Compiler.CompilerParameters(args, tempAssemblyFile);

                System.CodeDom.Compiler.CompilerResults cr =
                    cscp.CompileAssemblyFromFile(cparam, sources);

                foreach (System.CodeDom.Compiler.CompilerError error in cr.Errors)
                    Console.WriteLine("Error! '{0}'", error.ErrorText);

                try
                {
                    string projectFullName = cr.CompiledAssembly.FullName;
                    Regex verRegex = new Regex("Version=([\\d\\.]+)");
                    Match verMatch = verRegex.Match(projectFullName);
                    if (verMatch.Success)
                        projectVersion = verMatch.Groups[1].Value;
                }
                catch
                {
                    Console.WriteLine("Couldn't compile AssemblyInfo.cs");
                }

                // Clean up the temp file
                try
                {
                    if (File.Exists(tempAssemblyFile))
                        File.Delete(tempAssemblyFile);
                }
                catch
                {
                    Console.WriteLine("Error! '{0}'", e);
                }

            }
            */
            // Tell the user if there's a problem copying the file
            try
            {
                mkdirDashP(Path.GetDirectoryName(dest));
                File.Copy(source, dest, true);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }

            switch (project.Files.GetBuildAction(filename))
            {
                case BuildAction.Compile:
                    compiledFiles.Add(filename);
                    break;
                case BuildAction.Content:
                    contentFiles.Add(filename);
                    extraDistFiles.Add(filename);
                    break;
                case BuildAction.EmbeddedResource:
                    embeddedFiles.Add(filename);
                    break;
            }
        }

        // Set up references
        for (var refNum = 0; refNum < project.References.Count; refNum++)
        {
            var refr = project.References[refNum];
            var refAssembly = Assembly.Load(refr.Name);

            /* Determine which pkg-config (.pc) file refers to
               this assembly */

            SystemPackage package = null;

            if (packagesHash.ContainsKey(refr.Name))
            {
                package = packagesHash[refr.Name];
            }
            else
            {
                var assemblyFullName = string.Empty;
                if (refAssembly != null)
                    assemblyFullName = refAssembly.FullName;

                var assemblyFileName = string.Empty;
                if (assemblyFullName != string.Empty &&
                    assemblyFullNameToPath.ContainsKey(assemblyFullName)
                   )
                    assemblyFileName =
                        assemblyFullNameToPath[assemblyFullName];

                if (assemblyFileName != string.Empty &&
                    assemblyPathToPackage.ContainsKey(assemblyFileName)
                   )
                    package = assemblyPathToPackage[assemblyFileName];
            }

            /* If we know the .pc file and it is not "mono"
               (already in the path), add a -pkg: argument */

            if (package != null &&
                package.Name != "mono" &&
                !pkgLibs.Contains(package.Name)
               )
                pkgLibs.Add(package.Name);

            var fileRef =
                FindFileReference(refr.Name, (ProjectNode)refr.Parent);

            if (refr.LocalCopy ||
                solution.ProjectsTable.ContainsKey(refr.Name) ||
                fileRef != null ||
                refr.Path != null
               )
            {
                /* Attempt to copy the referenced lib to the
                   project's directory */

                var filename = refr.Name + ".dll";
                var source = filename;
                if (refr.Path != null)
                    source = Path.Combine(refr.Path, source);
                source = Path.Combine(project.FullPath, source);
                var dest = Path.Combine(projectDir, filename);

                /* Since we depend on this binary dll to build, we
                 * will add a compile- time dependency on the
                 * copied dll, and add the dll to the list of
                 * files distributed with this package
                 */

                binaryLibs.Add(refr.Name + ".dll");
                extraDistFiles.Add(refr.Name + ".dll");

                // TODO: Support copying .config.osx files
                // TODO: Support for determining native dependencies
                if (File.Exists(source + ".config"))
                {
                    File.Copy(source + ".config", Path.GetDirectoryName(dest), true);
                    extraDistFiles.Add(refr.Name + ".dll.config");
                }

                try
                {
                    File.Copy(source, dest, true);
                }
                catch (IOException)
                {
                    if (solution.ProjectsTable.ContainsKey(refr.Name))
                    {
                        /* If an assembly is referenced, marked for
                         * local copy, in the list of projects for
                         * this solution, but does not exist, put a
                         * target into the Makefile.am to build the
                         * assembly and copy it to this project's
                         * directory
                         */

                        var sourcePrj =
                            solution.ProjectsTable[refr.Name];

                        var target =
                            string.Format("{0}:\n" +
                                          "\t$(MAKE) -C ../{1}\n" +
                                          "\tln ../{2}/$@ $@\n",
                                filename,
                                sourcePrj.Name,
                                sourcePrj.Name);

                        localCopyTargets.Add(target);
                    }
                }
            }
            else if (!pkgLibs.Contains(refr.Name))
            {
                // Else, let's assume it's in the GAC or the lib path
                var assemName = string.Empty;
                var index = refr.Name.IndexOf(",");

                if (index > 0)
                    assemName = refr.Name.Substring(0, index);
                else
                    assemName = refr.Name;

                m_Kernel.Log.Write(string.Format(
                    "Warning: Couldn't find an appropriate assembly " +
                    "for reference:\n'{0}'", refr.Name
                ));
                systemLibs.Add(assemName);
            }
        }

        const string lineSep = " \\\n\t";
        var compiledFilesString = string.Empty;
        if (compiledFiles.Count > 0)
            compiledFilesString =
                lineSep + string.Join(lineSep, compiledFiles.ToArray());

        var embeddedFilesString = "";
        if (embeddedFiles.Count > 0)
            embeddedFilesString =
                lineSep + string.Join(lineSep, embeddedFiles.ToArray());

        var contentFilesString = "";
        if (contentFiles.Count > 0)
            contentFilesString =
                lineSep + string.Join(lineSep, contentFiles.ToArray());

        var extraDistFilesString = "";
        if (extraDistFiles.Count > 0)
            extraDistFilesString =
                lineSep + string.Join(lineSep, extraDistFiles.ToArray());

        var pkgLibsString = "";
        if (pkgLibs.Count > 0)
            pkgLibsString =
                lineSep + string.Join(lineSep, pkgLibs.ToArray());

        var binaryLibsString = "";
        if (binaryLibs.Count > 0)
            binaryLibsString =
                lineSep + string.Join(lineSep, binaryLibs.ToArray());

        var systemLibsString = "";
        if (systemLibs.Count > 0)
            systemLibsString =
                lineSep + string.Join(lineSep, systemLibs.ToArray());

        var localCopyTargetsString = "";
        if (localCopyTargets.Count > 0)
            localCopyTargetsString =
                string.Join("\n", localCopyTargets.ToArray());

        var monoPath = "";
        foreach (var runtimeLib in runtimeLibs) monoPath += ":`pkg-config --variable=libdir " + runtimeLib + "`";

        // Add the project name to the list of transformation
        // parameters
        var argList = new XsltArgumentList();
        argList.AddParam("projectName", "", project.Name);
        argList.AddParam("solutionName", "", solution.Name);
        argList.AddParam("assemblyName", "", projectAssemblyName);
        argList.AddParam("compiledFiles", "", compiledFilesString);
        argList.AddParam("embeddedFiles", "", embeddedFilesString);
        argList.AddParam("contentFiles", "", contentFilesString);
        argList.AddParam("extraDistFiles", "", extraDistFilesString);
        argList.AddParam("pkgLibs", "", pkgLibsString);
        argList.AddParam("binaryLibs", "", binaryLibsString);
        argList.AddParam("systemLibs", "", systemLibsString);
        argList.AddParam("monoPath", "", monoPath);
        argList.AddParam("localCopyTargets", "", localCopyTargetsString);
        argList.AddParam("projectVersion", "", projectVersion);
        argList.AddParam("hasAssemblyConfig", "", hasAssemblyConfig ? "true" : "");

        // Transform the templates
        transformToFile(Path.Combine(projectDir, "configure.ac"), argList, "/Autotools/ProjectConfigureAc");
        transformToFile(Path.Combine(projectDir, "Makefile.am"), argList, "/Autotools/ProjectMakefileAm");
        transformToFile(Path.Combine(projectDir, "autogen.sh"), argList, "/Autotools/ProjectAutogenSh");

        if (project.Type == ProjectType.Library)
            transformToFile(Path.Combine(projectDir, project.Name + ".pc.in"), argList, "/Autotools/ProjectPcIn");
        if (project.Type == ProjectType.Exe || project.Type == ProjectType.WinExe)
            transformToFile(Path.Combine(projectDir, project.Name.ToLower() + ".in"), argList,
                "/Autotools/ProjectWrapperScriptIn");
    }

    private void CleanProject(ProjectNode project)
    {
        m_Kernel.Log.Write("...Cleaning project: {0}", project.Name);
        var projectFile = Helper.MakeFilePath(project.FullPath, "Include", "am");
        Helper.DeleteIfExists(projectFile);
    }

    private void CleanSolution(SolutionNode solution)
    {
        m_Kernel.Log.Write("Cleaning Autotools make files for", solution.Name);

        var slnFile = Helper.MakeFilePath(solution.FullPath, "Makefile", "am");
        Helper.DeleteIfExists(slnFile);

        slnFile = Helper.MakeFilePath(solution.FullPath, "Makefile", "in");
        Helper.DeleteIfExists(slnFile);

        slnFile = Helper.MakeFilePath(solution.FullPath, "configure", "ac");
        Helper.DeleteIfExists(slnFile);

        slnFile = Helper.MakeFilePath(solution.FullPath, "configure");
        Helper.DeleteIfExists(slnFile);

        slnFile = Helper.MakeFilePath(solution.FullPath, "Makefile");
        Helper.DeleteIfExists(slnFile);

        foreach (var project in solution.Projects) CleanProject(project);

        m_Kernel.Log.Write("");
    }

    #endregion

    #region ITarget Members

    /// <summary>
    ///     Writes the specified kern.
    /// </summary>
    /// <param name="kern">The kern.</param>
    public void Write(Kernel kern)
    {
        if (kern == null) throw new ArgumentNullException("kern");
        m_Kernel = kern;
        m_Kernel.Log.Write("Parsing system pkg-config files");
        RunInitialization();

        const string streamName = "autotools.xml";
        var fqStreamName = string.Format("Prebuild.data.{0}", streamName);

        // Retrieve stream for the autotools template XML
        var autotoolsStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(fqStreamName);

        if (autotoolsStream == null)
        {
            /* 
             * try without the default namespace prepended, in
             * case prebuild.exe assembly was compiled with
             * something other than Visual Studio .NET
             */

            autotoolsStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(streamName);
            if (autotoolsStream == null)
            {
                var errStr =
                    string.Format("Could not find embedded resource file:\n" +
                                  "'{0}' or '{1}'", streamName, fqStreamName);

                m_Kernel.Log.Write(errStr);

                throw new TargetException(errStr);
            }
        }

        // Create an XML URL Resolver with default credentials
        xr = new XmlUrlResolver();
        xr.Credentials = CredentialCache.DefaultCredentials;

        // Load the autotools XML
        autotoolsDoc = new XmlDocument();
        autotoolsDoc.Load(autotoolsStream);

        /* rootDir is the filesystem location where the Autotools
         * build tree will be created - for now we'll make it
         * $PWD/autotools
         */

        var pwd = Directory.GetCurrentDirectory();
        //string pwd = System.Environment.GetEnvironmentVariable("PWD");
        //if (pwd.Length != 0)
        //{
        var rootDir = Path.Combine(pwd, "autotools");
        //}
        //else
        //{
        //    pwd = Assembly.GetExecutingAssembly()
        //}
        chkMkDir(rootDir);

        foreach (var solution in kern.Solutions)
        {
            m_Kernel.Log.Write(string.Format("Writing solution: {0}",
                solution.Name));
            WriteCombine(solution);
        }

        m_Kernel = null;
    }

    /// <summary>
    ///     Cleans the specified kern.
    /// </summary>
    /// <param name="kern">The kern.</param>
    public virtual void Clean(Kernel kern)
    {
        if (kern == null) throw new ArgumentNullException("kern");
        m_Kernel = kern;
        foreach (var sol in kern.Solutions) CleanSolution(sol);
        m_Kernel = null;
    }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name => "autotools";

    #endregion
}