using Prebuild.Core.Attributes;
using Prebuild.Core.Nodes;

namespace Prebuild.Core.Targets;

/// <summary>
/// </summary>
[Target("vs2008")]
public class VS2008Target : VSGenericTarget
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="VS2005Target" /> class.
    /// </summary>
    public VS2008Target()
    {
    }

    #endregion

    #region Fields

    /// <summary>
    ///     Gets or sets the solution version.
    /// </summary>
    /// <value>The solution version.</value>
    public override string SolutionVersion { get; } = "10.00";

    /// <summary>
    ///     Gets or sets the product version.
    /// </summary>
    /// <value>The product version.</value>
    public override string ProductVersion { get; } = "9.0.21022";

    /// <summary>
    ///     Gets or sets the schema version.
    /// </summary>
    /// <value>The schema version.</value>
    public override string SchemaVersion { get; } = "2.0";

    /// <summary>
    ///     Gets or sets the name of the version.
    /// </summary>
    /// <value>The name of the version.</value>
    public override string VersionName { get; } = "Visual Studio 2008";

    /// <summary>
    ///     Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public override VSVersion Version { get; } = VSVersion.VS90;

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public override string Name { get; } = "vs2008";

    protected override string GetToolsVersionXml(FrameworkVersion frameworkVersion)
    {
        switch (frameworkVersion)
        {
            case FrameworkVersion.v3_5:
                return "ToolsVersion=\"3.5\"";
            case FrameworkVersion.v3_0:
                return "ToolsVersion=\"3.0\"";
            default:
                return "ToolsVersion=\"2.0\"";
        }
    }

    public override string SolutionTag => "# Visual Studio 2008";

    #endregion
}