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

using System.Collections.Generic;

namespace Prebuild.Core.Nodes;

/// <summary>
///     Implements a specialized list of configuration nodes which allows for lookup via
///     configuration name and platform.
/// </summary>
public class ConfigurationNodeCollection : List<ConfigurationNode>
{
    #region Properties

    public ConfigurationNode this[string nameAndPlatform]
    {
        get
        {
            foreach (var configurationNode in this)
                if (configurationNode.NameAndPlatform == nameAndPlatform)
                    return configurationNode;

            return null;
        }

        set
        {
            // See if the node 
            var configurationNode = this[nameAndPlatform];

            if (configurationNode != null)
                this[IndexOf(configurationNode)] = value;
            else
                Add(value);
        }
    }

    #endregion
}