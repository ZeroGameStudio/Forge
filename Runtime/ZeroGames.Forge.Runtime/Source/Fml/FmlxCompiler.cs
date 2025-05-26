// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxCompiler
{

    public XDocument Compile(XDocument source)
    {
        XDocument result = new(source);

        return result;
    }
    
}


