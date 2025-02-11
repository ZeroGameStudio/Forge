// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler.Client;

public class PrimitiveDataType : IPrimitiveDataType
{
	public required string Name { get; init; }
	public required string Namespace { get; init; }
	public required EPrimitiveType PrimitiveType { get; init; }
	public required bool CanBeKey { get; init; }
}


