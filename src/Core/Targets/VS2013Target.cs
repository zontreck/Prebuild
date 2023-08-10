using Prebuild.Core.Attributes;
using Prebuild.Core.Nodes;

namespace Prebuild.Core.Targets;

/// <summary>
/// </summary>
[Target("vs2013")]
public class VS2013Target : VSGenericTarget
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="VS2005Target" /> class.
    /// </summary>
    public VS2013Target()
    {
    }

    #endregion

    #region Fields

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the solution version.
    /// </summary>
    /// <value>The solution version.</value>
    public override string SolutionVersion { get; } = "12.00";

    /// <summary>
    ///     Gets or sets the product version.
    /// </summary>
    /// <value>The product version.</value>
    public override string ProductVersion { get; } = "12.0.31101";

    /// <summary>
    ///     Gets or sets the schema version.
    /// </summary>
    /// <value>The schema version.</value>
    public override string SchemaVersion { get; } = "2.0";

    /// <summary>
    ///     Gets or sets the name of the version.
    /// </summary>
    /// <value>The name of the version.</value>
    public override string VersionName { get; } = "Visual Studio 2013";

    /// <summary>
    ///     Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public override VSVersion Version { get; } = VSVersion.VS12;

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public override string Name { get; } = "vs2013";

    protected override string GetToolsVersionXml(FrameworkVersion frameworkVersion)
    {
        switch (frameworkVersion)
        {
            case FrameworkVersion.v4_6_1:
            case FrameworkVersion.v4_6:
                return "ToolsVersion=\"14.0\"";
            case FrameworkVersion.v4_5_1:
            case FrameworkVersion.v4_5:
                return "ToolsVersion=\"12.0\"";
            case FrameworkVersion.v4_0:
            case FrameworkVersion.v3_5:
                return "ToolsVersion=\"4.0\"";
            case FrameworkVersion.v3_0:
                return "ToolsVersion=\"3.0\"";
            default:
                return "ToolsVersion=\"2.0\"";
        }
    }

    public override string SolutionTag => "# Visual Studio 2013";

    #endregion
}