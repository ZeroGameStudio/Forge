// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxDocumentSource(Type registryType, XDocument source) : IFmlDocumentSource
{
	public FmlxDocumentSource(Type registryType, Stream source) : this(registryType, XDocument.Load(source)){}
	
	public FmlDocument Document => field == default ? field = new FmlxCompiler().Compile(new(_registryType, _source)) : field;
	
	private readonly Type _registryType = registryType;
	private readonly XDocument _source = source;
}

public class FmlxDocumentSource<T>(XDocument source) : FmlxDocumentSource(typeof(T), source) where T : class, IRegistry
{
	public FmlxDocumentSource(Stream source) : this(XDocument.Load(source)){}
}


