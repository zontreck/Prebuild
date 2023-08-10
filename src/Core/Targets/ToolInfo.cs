using System;

namespace Prebuild.Core.Targets;

/// <summary>
/// </summary>
public struct ToolInfo
{
    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the GUID.
    /// </summary>
    /// <value>The GUID.</value>
    public string Guid { get; set; }

    /// <summary>
    ///     Gets or sets the file extension.
    /// </summary>
    /// <value>The file extension.</value>
    public string FileExtension { get; set; }

    public string LanguageExtension
    {
        get
        {
            switch (Name)
            {
                case "C#":
                    return ".cs";
                case "VisualBasic":
                    return ".vb";
                case "Boo":
                    return ".boo";
                default:
                    return ".cs";
            }
        }
    }

    /// <summary>
    ///     Gets or sets the XML tag.
    /// </summary>
    /// <value>The XML tag.</value>
    public string XmlTag { get; set; }

    /// <summary>
    ///     Gets or sets the import project property.
    /// </summary>
    /// <value>The ImportProject tag.</value>
    public string ImportProject { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolInfo" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="guid">The GUID.</param>
    /// <param name="fileExtension">The file extension.</param>
    /// <param name="xml">The XML.</param>
    /// <param name="importProject">The import project.</param>
    public ToolInfo(string name, string guid, string fileExtension, string xml, string importProject)
    {
        this.Name = name;
        this.Guid = guid;
        this.FileExtension = fileExtension;
        XmlTag = xml;
        this.ImportProject = importProject;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ToolInfo" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="guid">The GUID.</param>
    /// <param name="fileExtension">The file extension.</param>
    /// <param name="xml">The XML.</param>
    public ToolInfo(string name, string guid, string fileExtension, string xml)
    {
        this.Name = name;
        this.Guid = guid;
        this.FileExtension = fileExtension;
        XmlTag = xml;
        ImportProject = "$(MSBuildBinPath)\\Microsoft." + xml + ".Targets";
    }

    /// <summary>
    ///     Equals operator
    /// </summary>
    /// <param name="obj">ToolInfo to compare</param>
    /// <returns>true if toolInfos are equal</returns>
    public override bool Equals(object obj)
    {
        if (obj == null) throw new ArgumentNullException("obj");
        if (obj.GetType() != typeof(ToolInfo))
            return false;

        var c = (ToolInfo)obj;
        return Name == c.Name && Guid == c.Guid && FileExtension == c.FileExtension && ImportProject == c.ImportProject;
    }

    /// <summary>
    ///     Equals operator
    /// </summary>
    /// <param name="c1">ToolInfo to compare</param>
    /// <param name="c2">ToolInfo to compare</param>
    /// <returns>True if toolInfos are equal</returns>
    public static bool operator ==(ToolInfo c1, ToolInfo c2)
    {
        return c1.Name == c2.Name && c1.Guid == c2.Guid && c1.FileExtension == c2.FileExtension &&
               c1.ImportProject == c2.ImportProject && c1.XmlTag == c2.XmlTag;
    }

    /// <summary>
    ///     Not equals operator
    /// </summary>
    /// <param name="c1">ToolInfo to compare</param>
    /// <param name="c2">ToolInfo to compare</param>
    /// <returns>True if toolInfos are not equal</returns>
    public static bool operator !=(ToolInfo c1, ToolInfo c2)
    {
        return !(c1 == c2);
    }

    /// <summary>
    ///     Hash Code
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ Guid.GetHashCode() ^ FileExtension.GetHashCode() ^ ImportProject.GetHashCode() ^
               XmlTag.GetHashCode();
    }
}