// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentSource(XDocument source, bool fmlx = true) : IFmlDocumentSource
{
	public FmlDocumentSource(Stream source, bool fmlx = true) : this(XDocument.Load(source), fmlx){}
	
	public XDocument Document { get; } = fmlx ? new FmlxCompiler().Compile(source) : source;
}


