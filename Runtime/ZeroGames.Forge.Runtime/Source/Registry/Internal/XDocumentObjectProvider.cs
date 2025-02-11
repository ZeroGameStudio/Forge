// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

internal class XDocumentObjectProvider(XDocument source) : IXDocumentProvider
{
	public XDocument Document { get; } = source;
}


