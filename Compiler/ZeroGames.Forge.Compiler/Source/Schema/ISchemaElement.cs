// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface ISchemaElement : INameProvider
{
	ISchema Schema { get; }
	IReadOnlyList<IMetadata> Metadatas { get; }
}


