using System;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

[DataNode("DatabaseReference")]
public class DatabaseReferenceNode : DataNode
{
    public string Name { get; internal set; }

    public Guid ProviderId { get; internal set; }

    public string ConnectionString { get; internal set; }

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
                    ProviderId = new Guid(DatabaseProviders.SqlServerCe35);
                    break;
                case "System.Data.OleDb":
                    ProviderId = new Guid(DatabaseProviders.OleDb);
                    break;
                case "System.Data.OracleClient":
                    ProviderId = new Guid(DatabaseProviders.OracleClient);
                    break;
                case "System.Data.SqlClient":
                    ProviderId = new Guid(DatabaseProviders.SqlClient);
                    break;
                case "System.Data.Odbc":
                    ProviderId = new Guid(DatabaseProviders.Odbc);
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

    public override void Write(XmlDocument doc, XmlElement current)
    {
        XmlElement main = doc.CreateElement("DatabaseReference");
        main.SetAttribute("name", Name);

        string provider = "";
        string providerId = "";


        switch (ProviderId.ToString())
        {
            case DatabaseProviders.Odbc:
                {
                    provider = "System.Data.Odbc";
                    break;
                }
            case DatabaseProviders.OracleClient:
            {
                provider = "System.Data.OracleClient";
                break;
            }
            case DatabaseProviders.SqlClient:
                {
                    provider = "System.Data.SqlClient";
                    break;
                }
            case DatabaseProviders.OleDb:
                {
                    provider = "System.Data.OleDb";
                    break;
                }
            case DatabaseProviders.SqlServerCe35:
                {
                    provider = "Microsoft.SqlServerCe.Client.3.5";
                    break;
                }
            default:
                {
                    providerId = ProviderId.ToString();
                    provider = null;
                    break;
                }
        }
        if(provider!= null)
            main.SetAttribute("providerName", provider);
        else
        {
            main.SetAttribute("providerId", providerId);
        }

        var conn = doc.CreateAttribute("connectionString");
        conn.InnerText = ConnectionString;
        main.Attributes.Append(conn);

        current.AppendChild(main);
    }
}

public class DatabaseProviders
{
    public const string SqlServerCe35 = ("7C602B5B-ACCB-4acd-9DC0-CA66388C1533");
    public const string OleDb = ("7F041D59-D76A-44ed-9AA2-FBF6B0548B80");
    public const string OracleClient = ("8F5C5018-AE09-42cf-B2CC-2CCCC7CFC2BB");
    public const string SqlClient = ("91510608-8809-4020-8897-FBA057E22D54");
    public const string Odbc = ("C3D4F4CE-2C48-4381-B4D6-34FA50C51C86");
}