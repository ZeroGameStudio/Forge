// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxDocumentSource(Type registryType, XDocument source, bool fmlx = true) : IForgeDocumentSource
{
	public FmlxDocumentSource(Type registryType, Stream source, bool fmlx = true) : this(registryType, XDocument.Load(source), fmlx){}
	
	public ForgeDocument Document { get; } = new (registryType, fmlx ? new FmlxCompiler().Compile(source) : new(source));
}

public class FmlxDocumentSource<T>(XDocument source, bool fmlx = true) : FmlxDocumentSource(typeof(T), source, fmlx) where T : class, IRegistry
{
	public FmlxDocumentSource(Stream source, bool fmlx = true) : this(XDocument.Load(source), fmlx){}
}


