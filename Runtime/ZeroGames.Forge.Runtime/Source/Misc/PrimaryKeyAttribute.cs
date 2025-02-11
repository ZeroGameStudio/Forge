// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PrimaryKeyAttribute(params string[] components) : Attribute
{
	public IReadOnlyList<string> Components { get; } = components;
}


