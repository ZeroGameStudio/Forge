// Copyright Zero Games. All Rights Reserved.

using System.Reflection;

namespace ZeroGames.Forge.Runtime;

public class RegistryMetadata
{
	
	public static void Get(Type registryType, out RegistryMetadata metadata)
	{
		if (!registryType.IsClass || !registryType.IsAssignableTo(typeof(IRegistry)))
		{
			throw new ArgumentOutOfRangeException(nameof(registryType));
		}
		
		lock (_cacheLock)
		{
			if (_cache.TryGetValue(registryType, out var existing))
			{
				metadata = existing;
				return;
			}
			
			List<PropertyInfo> imports = [];
			List<PropertyInfo> repositories = [];
			List<PropertyInfo> autoIndices = [];

			foreach (var property in registryType.GetProperties())
			{
				Type propertyType = property.PropertyType;
				if (property.GetCustomAttribute<ImportAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException($"Property {property.Name} is readonly.");
					}
					
					if (!propertyType.IsAssignableTo(typeof(IRegistry)))
					{
						throw new InvalidOperationException($"Property type {propertyType.Name} does not implement IRegistry.");
					}
				
					imports.Add(property);
				}
				else if (property.GetCustomAttribute<RepositoryAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException($"Property {property.Name} is readonly.");
					}
					
					if (!propertyType.GetInterfaces().Append(propertyType).Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRepository<,>)))
					{
						throw new InvalidOperationException($"Property type {propertyType.Name} does not implement IRepository<,>.");
					}
				
					repositories.Add(property);
				}
				else if (property.GetCustomAttribute<AutoIndexAttribute>() is not null)
				{
					if (property.SetMethod is null)
					{
						throw new InvalidOperationException($"Property {property.Name} is readonly.");
					}
					
					if (!propertyType.IsAssignableTo(typeof(IIndex)))
					{
						throw new InvalidOperationException($"Property type {propertyType.Name} does not implement IIndex.");
					}
				
					autoIndices.Add(property);
				}
			}

			metadata = new()
			{
				RegistryType = registryType,
				Imports = imports,
				Repositories = repositories,
				AutoIndices = autoIndices,
			};
			_cache[registryType] = metadata;
		}
	}
	
	public static void Get<T>(out RegistryMetadata metadata) where T : class, IRegistry => Get(typeof(T), out metadata);
    
	public required Type RegistryType { get; init; }
    public required IReadOnlyList<PropertyInfo> Imports { get; init; }
    public required IReadOnlyList<PropertyInfo> Repositories { get; init; }
    public required IReadOnlyList<PropertyInfo> AutoIndices { get; init; }
    
    private static readonly Dictionary<Type, RegistryMetadata> _cache = new();
    private static readonly Lock _cacheLock = new();
    
}



