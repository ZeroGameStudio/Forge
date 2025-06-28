// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxCompiler
{
    public XDocument Compile(XDocument source)
    {
        XDocument result = new(source);
        
        /*
         * TODO: Compile fmlx document object to fml document object.
         * 1. Map key as attribute.
         * 2. Serialized struct.
         * 3. Serialized container.
         * 4. Untyped reference/struct.
         */

        return result;
    }

}


