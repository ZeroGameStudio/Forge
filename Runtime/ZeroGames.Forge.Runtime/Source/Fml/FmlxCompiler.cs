// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxCompiler
{

    public XDocument Compile(XDocument source)
    {
        XDocument result = new(source);
        
        // TODO: Compile fmlx document object to fml document object.

        return result;
    }
    
}


