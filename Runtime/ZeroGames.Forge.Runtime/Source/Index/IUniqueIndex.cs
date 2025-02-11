// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace ZeroGames.Forge.Runtime;

public interface IUniqueIndex<in TKey, TEntity> : IIndex
	where TKey : notnull
	where TEntity : class, IEntity
{
	bool TryGetEntity(TKey key, [NotNullWhen(true)] out TEntity? entity);
	TEntity this[TKey key] { get; }
}


