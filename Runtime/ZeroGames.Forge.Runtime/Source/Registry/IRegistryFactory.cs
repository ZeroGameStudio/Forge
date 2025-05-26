// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

public interface IRegistryFactory
{
	object Create(Type registryType, IFmlDocumentSource source, params IEnumerable<IRegistry> imports);
}

public static class RegistryFactoryExtensions
{

	public static T Create<T>(this IRegistryFactory @this, IFmlDocumentSource source, params IEnumerable<IRegistry> imports) where T : class, IRegistry
		=> (T)@this.Create(typeof(T), source, imports);
	
}


