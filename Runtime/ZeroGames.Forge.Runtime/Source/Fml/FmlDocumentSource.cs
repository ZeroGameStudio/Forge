// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentSource(Type registryType, XDocument source) : IForgeDocumentSource
{
	public FmlDocumentSource(Type registryType, Stream source) : this(registryType, XDocument.Load(source)){}
	
	public ForgeDocument Document { get; } = new (registryType, new(source));
}

public class FmlDocumentSource<T>(XDocument source) : FmlxDocumentSource(typeof(T), source) where T : class, IRegistry
{
	public FmlDocumentSource(Stream source) : this(XDocument.Load(source)){}
}


