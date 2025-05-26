// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmltDocumentSource(XDocument source) : IFmltDocumentSource
{
    public XDocument Document { get; } = source;
}


