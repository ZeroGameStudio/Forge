// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface IUserDefinedDataType : IDataType, ISchemaElement
{
	IDataType? BaseType { get; }
}


