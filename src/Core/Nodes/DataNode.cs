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

using System.IO;
using System.Xml;
using Prebuild.Core.Interfaces;

namespace Prebuild.Core.Nodes;

/// <summary>
/// </summary>
public abstract class DataNode : IDataNode
{
    #region Fields

    #endregion

    #region IDataNode Members

    /// <summary>
    ///     Gets or sets the parent.
    /// </summary>
    /// <value>The parent.</value>
    public virtual IDataNode Parent { get; set; }

    public string[] WebTypes { get; } = { "aspx", "ascx", "master", "ashx", "asmx" };

    /// <summary>
    ///     Parses the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public virtual void Parse(XmlNode node)
    {
    }

    public BuildAction GetBuildActionByFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        foreach (var type in WebTypes)
            if (extension == type)
                return BuildAction.Content;
        return BuildAction.Compile;
    }

    /// <summary>
    ///     Parses the file type to figure out what type it is
    /// </summary>
    /// <returns></returns>
    public SubType GetSubTypeByFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        var designer = string.Format(".designer{0}", extension);
        var path = fileName.ToLower();
        if (extension == ".resx")
            return SubType.Designer;
        if (path.EndsWith(".settings"))
            return SubType.Settings;
        foreach (var type in WebTypes)
            if (path.EndsWith(type))
                return SubType.CodeBehind;
        return SubType.Code;
    }

    #endregion
}