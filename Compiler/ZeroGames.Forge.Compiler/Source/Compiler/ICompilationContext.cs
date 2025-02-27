﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface ICompilationContext
{
	IPrimitiveDataType GetPrimitiveDataType(string name);
	ISchema GetSchema(SchemaSourceUri uri);

	VoidDataType VoidDataType { get; }
	
	IGenericContainerDataType GenericListType { get; }
	IGenericContainerDataType GenericSetType { get; }
	IGenericContainerDataType GenericMapType { get; }
	IGenericContainerDataType GenericOptionalType { get; }
}


