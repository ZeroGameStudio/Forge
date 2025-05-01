// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

public interface IRegistryFactory
{
	object Create(Type registryType, IEnumerable<IXDocumentProvider> sources, IEnumerable<IRegistry> imports);
}

public static class RegistryFactoryExtensions
{
	
	public static T Create<T>(this IRegistryFactory @this, IEnumerable<IXDocumentProvider> sources, IEnumerable<IRegistry> imports) where T : class, IRegistry
		=> (T)@this.Create(typeof(T), sources, imports);
	
}


