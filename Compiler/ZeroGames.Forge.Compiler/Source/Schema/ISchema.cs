// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface ISchema : ISchemaElement, INamespaceProvider
{
	SchemaSourceUri Uri { get; }
	IReadOnlyDictionary<string, ISchema> ImportMap { get; }
	IReadOnlyList<IUserDefinedDataType> DataTypes { get; }
	
	ISchema ISchemaElement.Schema => this;
}


