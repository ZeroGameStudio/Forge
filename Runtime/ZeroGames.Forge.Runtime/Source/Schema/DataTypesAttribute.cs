﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

[AttributeUsage(AttributeTargets.Class)]
public class DataTypesAttribute(params Type[] dataTypes) : Attribute
{
	public Type this[string typeName] => _dataTypeByTypeName[typeName];
	public IEnumerable<Type> Types => _dataTypeByTypeName.Values;
	private readonly Dictionary<string, Type> _dataTypeByTypeName = dataTypes.ToDictionary(t => t.Name, t => t);
}


