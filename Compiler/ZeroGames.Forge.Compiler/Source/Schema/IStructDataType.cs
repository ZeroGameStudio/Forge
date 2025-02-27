﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler;

public interface IStructDataType : ICompositeDataType
{
	new IStructDataType? BaseType { get; }
	
	ICompositeDataType? ICompositeDataType.BaseType => BaseType;
}


