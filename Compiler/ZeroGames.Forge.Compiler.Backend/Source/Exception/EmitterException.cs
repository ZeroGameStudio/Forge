// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler.Backend;

public class EmitterException(string? message = null, Exception? inner = null) : Exception(message ?? "Failed to compile schema.", inner)
{
	public EmitterException(Exception? inner) : this(null, inner){}
}


