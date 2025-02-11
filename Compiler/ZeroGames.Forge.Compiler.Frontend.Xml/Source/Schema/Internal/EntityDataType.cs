// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Compiler.Frontend.Xml;

internal sealed class EntityDataType : CompositeDataTypeBase, IEntityDataType
{
	public IEntityDataType? BaseType { get; private set; }
	public required Func<IEntityDataType?> BaseTypeFactory { private get; init; }
	public required IReadOnlyList<IProperty> PrimaryKeyComponents { get; init; }

	protected override bool InternalSetupDependencies()
	{
		BaseType = BaseTypeFactory();
		return true;
	}
	
	protected override IEntityDataType? InternalBaseType => BaseType;
}


