using Prebuild.Core.Attributes;
using Prebuild.Core.Nodes;

namespace Prebuild.Core.Targets;

/// <summary>
/// </summary>
[Target("vs2017")]
public class VS2017Target : VSGenericTarget
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="VS2012Target" /> class.
    /// </summary>
    public VS2017Target()
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
    public override string ProductVersion { get; } = "15.9.50";

    /// <summary>
    ///     Gets or sets the schema version.
    /// </summary>
    /// <value>The schema version.</value>
    public override string SchemaVersion { get; } = "2.0";

    /// <summary>
    ///     Gets or sets the name of the version.
    /// </summary>
    /// <value>The name of the version.</value>
    public override string VersionName { get; } = "Visual Studio 17";

    /// <summary>
    ///     Gets or sets the version.
    /// </summary>
    /// <value>The version.</value>
    public override VSVersion Version { get; } = VSVersion.VS17;

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public override string Name { get; } = "vs2017";

    protected override string GetToolsVersionXml(FrameworkVersion frameworkVersion)
    {
        switch (frameworkVersion)
        {
            case FrameworkVersion.v4_8:
            case FrameworkVersion.v4_7_2:
            case FrameworkVersion.v4_7_1:
            case FrameworkVersion.v4_7:
                return "ToolsVersion=\"15.0\"";
            case FrameworkVersion.v4_6_2:
            case FrameworkVersion.v4_6_1:
            case FrameworkVersion.v4_6:
                return "ToolsVersion=\"14.0\"";
            case FrameworkVersion.v4_5_2:
                return "ToolsVersion=\"12.0\"";
            case FrameworkVersion.v4_5_1:
            case FrameworkVersion.v4_5:
            case FrameworkVersion.v4_0:
            case FrameworkVersion.v3_5:
                return "ToolsVersion=\"4.0\"";
            case FrameworkVersion.v3_0:
                return "ToolsVersion=\"3.0\"";
            default:
                return "ToolsVersion=\"2.0\"";
        }
    }

    public override string SolutionTag => "# Visual Studio 17";

    #endregion
}