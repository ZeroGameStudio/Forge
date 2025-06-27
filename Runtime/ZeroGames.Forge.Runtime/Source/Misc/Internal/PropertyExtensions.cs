// Copyright Zero Games. All Rights Reserved.

using System.Reflection;

namespace ZeroGames.Forge.Runtime;

public static class PropertyExtensions
{

	public static bool IsNullable(this PropertyInfo @this)
	{
		if (@this.PropertyType.IsValueType)
		{
			return Nullable.GetUnderlyingType(@this.PropertyType) is not null;
		}
		else
		{
			return _nullabilityInfoContext.Create(@this).ReadState != NullabilityState.NotNull;
		}
	}

	public static bool IsNotNull(this PropertyInfo @this) => !@this.IsNullable();
	
	private static readonly NullabilityInfoContext _nullabilityInfoContext = new();

}


