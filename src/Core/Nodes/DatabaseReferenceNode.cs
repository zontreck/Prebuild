using System;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

[DataNode("DatabaseReference")]
public class DatabaseReferenceNode : DataNode
{
    public string Name { get; private set; }

    public Guid ProviderId { get; private set; }

    public string ConnectionString { get; private set; }

    public override void Parse(XmlNode node)
    {
        Name = Helper.AttributeValue(node, "name", Name);

        var providerName = Helper.AttributeValue(node, "providerName", string.Empty);
        if (providerName != null)
            switch (providerName)
            {
                // digitaljeebus: pulled from HKLM\SOFTWARE\Microsoft\VisualStudio\9.0\DataProviders\*
                // Not sure if these will help other operating systems, or if there's a better way.
                case "Microsoft.SqlServerCe.Client.3.5":
                    ProviderId = new Guid("7C602B5B-ACCB-4acd-9DC0-CA66388C1533");
                    break;
                case "System.Data.OleDb":
                    ProviderId = new Guid("7F041D59-D76A-44ed-9AA2-FBF6B0548B80");
                    break;
                case "System.Data.OracleClient":
                    ProviderId = new Guid("8F5C5018-AE09-42cf-B2CC-2CCCC7CFC2BB");
                    break;
                case "System.Data.SqlClient":
                    ProviderId = new Guid("91510608-8809-4020-8897-FBA057E22D54");
                    break;
                case "System.Data.Odbc":
                    ProviderId = new Guid("C3D4F4CE-2C48-4381-B4D6-34FA50C51C86");
                    break;

                default:
                    throw new ArgumentOutOfRangeException("providerName", providerName,
                        "Could not provider name to an id.");
            }
        else
            ProviderId = new Guid(Helper.AttributeValue(node, "providerId", Guid.Empty.ToString("B")));

        ConnectionString = Helper.AttributeValue(node, "connectionString", ConnectionString);

        base.Parse(node);
    }
}