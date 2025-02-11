// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum)]
public class SchemaAttribute(Type schema) : Attribute
{
	public Type Schema { get; } = schema;
}


