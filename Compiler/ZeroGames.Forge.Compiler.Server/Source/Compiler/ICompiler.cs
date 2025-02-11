// Copyright Zero Games. All Rights Reserved.

using ZeroGames.Forge.Compiler.Backend;

namespace ZeroGames.Forge.Compiler.Server;

public interface ICompiler
{
	public static ICompiler Create(in CompilerOptions options) => new Compiler(options);
	
	IAsyncEnumerable<CompilationUnitResult> CompileAsync(IReadOnlySet<SchemaSourceUri> sources);
}


