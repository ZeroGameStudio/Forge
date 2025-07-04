﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

public interface IRegistryFactory
{
	object Create(IFmlDocumentSource source, params IEnumerable<IRegistry> imports);
}

public static class RegistryFactoryExtensions
{

	public static T Create<T>(this IRegistryFactory @this, IFmlDocumentSource source, params IEnumerable<IRegistry> imports) where T : class, IRegistry
		=> @this.Create(source, imports) as T ?? throw new ArgumentOutOfRangeException(nameof(source));
	
}


