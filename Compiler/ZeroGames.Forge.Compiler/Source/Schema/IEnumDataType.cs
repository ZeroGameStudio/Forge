// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface IEnumDataType : IUserDefinedDataType
{
	IPrimitiveDataType UnderlyingType { get; }
	IReadOnlyList<IEnumElement> Elements { get; }
}


