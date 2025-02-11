// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoIndexAttribute(string firstKeyName, params string[] remainingKeyNames) : Attribute
{
	public IReadOnlyList<string> Keys { get; } = [ firstKeyName, ..remainingKeyNames ];
}


