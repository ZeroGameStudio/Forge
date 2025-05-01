// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class XDocumentObjectProvider(XDocument source) : IXDocumentProvider
{
	public XDocument Document { get; } = source;
}


