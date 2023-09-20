using System;
using System.Collections.Generic;
using System.Xml;
using Prebuild.Core.Attributes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Nodes;

[DataNode("DatabaseProject")]
public class DatabaseProjectNode : DataNode
{
    private readonly List<AuthorNode> authors = new();
    private readonly List<DatabaseReferenceNode> references = new();

    public Guid Guid { get; } = Guid.NewGuid();

    public string Name { get; internal set; }

    public string Path { get; internal set; }

    public string FullPath { get; internal set; }

    public IEnumerable<DatabaseReferenceNode> References => references;

    public override void Parse(XmlNode node)
    {
        Name = Helper.AttributeValue(node, "name", Name);
        Path = Helper.AttributeValue(node, "path", Name);

        try
        {
            FullPath = Helper.ResolvePath(Path);
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

                if (dataNode == null)
                    continue;

                if (dataNode is AuthorNode)
                    authors.Add((AuthorNode)dataNode);
                else if (dataNode is DatabaseReferenceNode)
                    references.Add((DatabaseReferenceNode)dataNode);
            }
        }
        finally
        {
            Kernel.Instance.CurrentWorkingDirectory.Pop();
        }

        base.Parse(node);
    }

    public override void Write(XmlDocument doc, XmlElement current)
    {

        XmlElement main = doc.CreateElement("DatabaseProject");
        main.SetAttribute("name", Name);
        main.SetAttribute("path", Path);


        foreach(AuthorNode author in authors)
        {
            author.Write(doc, main);
        }
        foreach(DatabaseReferenceNode databaseReference in references)
        {
            databaseReference.Write(doc, main);
        }

        current.AppendChild(main);
    }
}