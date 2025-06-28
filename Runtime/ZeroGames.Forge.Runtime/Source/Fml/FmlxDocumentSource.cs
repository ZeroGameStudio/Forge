// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxDocumentSource(Type registryType, XDocument source) : IForgeDocumentSource
{
	public FmlxDocumentSource(Type registryType, Stream source) : this(registryType, XDocument.Load(source)){}
	
	public ForgeDocument Document => field == default ? field = new FmlxCompiler().Compile(new(registryType, source)) : field;
}

public class FmlxDocumentSource<T>(XDocument source) : FmlxDocumentSource(typeof(T), source) where T : class, IRegistry
{
	public FmlxDocumentSource(Stream source) : this(XDocument.Load(source)){}
}


