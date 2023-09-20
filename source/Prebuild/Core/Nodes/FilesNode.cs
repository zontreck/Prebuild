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
using System.Collections.Specialized;
using System.Xml;
using Prebuild.Core.Attributes;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
[DataNode("Files")]
public class FilesNode : DataNode
{
    #region Fields


    private readonly Dictionary<string, FileNode> m_Files = new();
    private readonly Dictionary<string, MatchNode> m_Matches = new();

    #endregion

    #region Properties

    public int Count => m_Files.Count;

    public int CopyFiles
    {
        get
        {
            int cur = 0;
            foreach (var item in m_Files)
            {
                if(item.Value.BuildAction == BuildAction.Copy) cur++;
            }
            foreach(var item in m_Matches)
            {
                if(item.Value.BuildAction == BuildAction.Copy) cur++;
            }

            return cur;
        }
    }

    public string[] Destinations
    {
        get
        {
            List<string> dests = new();
            foreach(var item in m_Matches)
            {
                dests.Add(item.Value.DestinationPath);
            }

            return dests.ToArray();
        }
    }

    #endregion

    #region Public Methods

    public BuildAction GetBuildAction(string file)
    {
        if(m_Files.ContainsKey(file))
        {
            return m_Files[file].BuildAction;
        }
        if(m_Matches.ContainsKey(file))
        {
            return (BuildAction)m_Matches[file].BuildAction;
        }
        return BuildAction.Compile;
    }

    public string GetDestinationPath(string file)
    {
        if (!m_Matches.ContainsKey(file)) return null;
        return m_Matches[file].DestinationPath;
    }

    public string[] SourceFiles(string dest)
    {
        List<string> files = new();
        foreach(MatchNode node in m_Matches.Values)
        {
            if (node.DestinationPath.Equals(dest))
            {
                files.AddRange(node.Files);
            }
        }
        return files.ToArray();
    }

    public CopyToOutput GetCopyToOutput(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].CopyToOutput;
        if(m_Matches.ContainsKey(file))return m_Matches[file].CopyToOutput;
        return CopyToOutput.Never;
    }

    public bool GetIsLink(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].IsLink;
        if (m_Matches.ContainsKey(file)) return m_Matches[file].IsLink;
        return false;
    }

    public bool Contains(string file)
    {
        return m_Files.ContainsKey(file) || m_Matches.ContainsKey(file);
    }

    public string GetLinkPath(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].LinkPath;
        if (m_Matches.ContainsKey(file)) return m_Matches[file].LinkPath;
        return string.Empty;
    }

    public SubType GetSubType(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].SubType;
        if (m_Matches.ContainsKey(file)) return (SubType)m_Matches[file].SubType;

        return SubType.Code;
    }

    public string GetResourceName(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].ResourceName;
        if (m_Matches.ContainsKey(file)) return m_Matches[file].ResourceName;

        return string.Empty;
    }

    public bool GetPreservePath(string file)
    {
        if (m_Files.ContainsKey(file)) return m_Files[file].PreservePath;
        if (m_Matches.ContainsKey(file)) return m_Matches[file].PreservePath;

        return false;
    }

    public override void Parse(XmlNode node)
    {
        if (node == null) throw new ArgumentNullException("node");
        foreach (XmlNode child in node.ChildNodes)
        {
            var dataNode = Kernel.Instance.ParseNode(child, this);
            if (dataNode is FileNode fn)
            {
                if (fn.IsValid)
                    if (!m_Files.ContainsKey(fn.Path))
                    {
                        m_Files.Add(fn.Path, fn);
                    }
            }
            else if (dataNode is MatchNode mn)
            {
                foreach (var file in mn.Files)
                {
                    m_Matches.Add(file, mn);
                    
                }
            }
        }
    }

    public override void Write(XmlDocument doc, XmlElement current)
    {
        XmlElement main = doc.CreateElement("Files");
        foreach(FileNode fi in m_Files.Values)
        {
            fi.Write(doc, main);
        }

        foreach(MatchNode mn in m_Matches.Values)
        {
            mn.Write(doc, main);
        }


        current.AppendChild(main);
    }

    // TODO: Check in to why StringCollection's enumerator doesn't implement
    // IEnumerator?
    public IEnumerator<string> GetEnumerator()
    {
        List<string> concat = new();
        concat.AddRange(m_Files.Keys);
        concat.AddRange(m_Matches.Keys);

        return concat.GetEnumerator();
    }

    #endregion
}