// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler.Frontend.Xml;

internal sealed class EnumElement : SchemaElementBase, IEnumElement
{
	public required Int128 Value { get; init; }
}


