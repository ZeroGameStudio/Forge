// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

internal class RepositoryFactory
{

	public delegate IEntity? GetEntityDelegate(Type type, object primaryKey);
	public delegate void FinishInitializationDelegate(GetEntityDelegate getEntity);

	public IInitializingRepository Create(IRegistry registry, Type entityType, out FinishInitializationDelegate finishInitialization)
	{
		IInitializingRepository repository = AllocateRepository(registry, entityType);
		finishInitialization = _ => (repository as INotifyInitialization)?.PostInitialize();
		return repository;
	}
	
	public IInitializingRepository Create(IRegistry registry, Type entityType, XElement repositoryElement, out FinishInitializationDelegate finishInitialization)
	{
		Logger?.Invoke(ELogVerbosity.Log, $"Creating {entityType.Name}Repository...");
		
		IInitializingRepository repository = AllocateRepository(registry, entityType);
		EntityMetadata.Get(entityType, out var metadata);

		Logger?.Invoke(ELogVerbosity.Log, "Allocating entities...");
		Dictionary<IEntity, XElement> pendingInitializedEntities = [];
		foreach (var entityElement in repositoryElement.Elements())
		{
			if (entityElement.Name != entityType.Name)
			{
				throw new InvalidOperationException($"Entity element name {entityElement.Name} mismatch to required {entityType.Name}.");
			}
			
			var entity = (IEntity?)Activator.CreateInstance(entityType);
			if (entity is null)
			{
				throw new InvalidOperationException($"Create instance of type {entityType.Name} failed.");
			}

			foreach (var property in metadata.PrimaryKeyComponents)
			{
				XElement? propertyElement = entityElement.Element(property.Name);
				if (propertyElement is null)
				{
					throw new InvalidOperationException($"Entity property {property.Name} not found.");
				}
				
				object value = SerializePrimitive(property.PropertyType, propertyElement.Value);
				property.SetValue(entity, value);
			}

			bool @abstract = entityElement.Attribute(FmlSyntax.ABSTRACT_ATTRIBUTE_NAME) is { } abstractAttr && bool.Parse(abstractAttr.Value);
			repository.RegisterEntity(entity, @abstract);
			pendingInitializedEntities[entity] = entityElement;
			
			Logger?.Invoke(ELogVerbosity.Verbose, $"Allocated entity {entityType.Name}({entity.PrimaryKey}).");
		}

		finishInitialization = getEntity =>
		{
			Logger?.Invoke(ELogVerbosity.Log, $"Fixing up entities for repository {repository.Name}...");
			
			HashSet<IEntity> initializedEntities = [];
			Dictionary<IEntity, Dictionary<PropertyInfo, XElement?>> entityPropertyElementLookup = [];

			void InitializeEntity(IEntity entity, XElement entityElement)
			{
				if (!initializedEntities.Add(entity))
				{
					return;
				}
				
				Logger?.Invoke(ELogVerbosity.Verbose, $"Fixing up entity {entityType.Name}({entity.PrimaryKey})...");
				
				// If entity extends another entity, then the base entity must get initialized first (recursively).
				string? baseEntityReference = entityElement.Attribute(FmlSyntax.EXTENDS_ATTRIBUTE_NAME)?.Value;
				IEntity? baseEntity = null;
				if (!string.IsNullOrWhiteSpace(baseEntityReference))
				{
					string[] rawComponents = baseEntityReference.Split(entityElement.Attribute(FmlSyntax.EXTENDS_SEP_ATTRIBUTE_NAME)?.Value ?? FmlSyntax.DEFAULT_REFERENCE_SEP);
					object primaryKey = MakePrimaryKey(metadata, rawComponents);
					repository.TryGetEntity(primaryKey, true, out baseEntity);
					if (baseEntity is null || baseEntity.GetType() != entityType)
					{
						throw new InvalidOperationException($"Base ({primaryKey}) not found for entity '{entity.PrimaryKey}'.");
					}
					
					InitializeEntity(baseEntity, pendingInitializedEntities[baseEntity]);
				}

				// Now all base entities on the inheritance chain is initialized so we can initialize this entity.
				bool @abstract = entityElement.Attribute(FmlSyntax.ABSTRACT_ATTRIBUTE_NAME) is { } abstractAttr && bool.Parse(abstractAttr.Value);
				if (!@abstract)
				{
					(entity as INotifyInitialization)?.PreInitialize();
				}
				
				foreach (var property in metadata.RemainingProperties)
				{
					XElement? propertyElement = entityElement.Element(property.Name);
					if (propertyElement is null)
					{
						if (!@abstract && baseEntity is null)
						{
							throw new InvalidOperationException($"Missing property {property.Name} on entity '{entity.PrimaryKey}'.");
						}

						if (baseEntity is not null)
						{
							try
							{
								propertyElement = entityPropertyElementLookup[baseEntity][property];
							}
							catch (KeyNotFoundException ex)
							{
								throw new InvalidOperationException($"Recursive inheritance detected on entity '{entity.PrimaryKey}'", ex);
							}
						}
					}

					if (!@abstract)
					{
						if (propertyElement is null)
						{
							throw new InvalidOperationException($"Missing property {property.Name} on entity '{entity.PrimaryKey}'.");
						}
						
						object? value = Serialize(property.PropertyType, propertyElement, property.IsNotNull() ? ReturnNotNull.True : ReturnNotNull.False, getEntity);
						property.SetValue(entity, value);
					}
					
					if (!entityPropertyElementLookup.TryGetValue(entity, out var lookup))
					{
						lookup = [];
						entityPropertyElementLookup[entity] = lookup;
					}
					
					lookup[property] = propertyElement;
				}

				if (!@abstract)
				{
					(entity as INotifyInitialization)?.PostInitialize();
				}
			}
			
			// NOTE: pendingInitializedEntities is set up before merge so there won't be child entities.
			foreach (var (entity, entityElement) in pendingInitializedEntities)
			{
				InitializeEntity(entity, entityElement);
			}

			(repository as INotifyInitialization)?.PostInitialize();
		};

		return repository;
	}
	
	public required IReadOnlyDictionary<Type, Func<string, object>> PrimitiveSerializerMap { private get; init; }

	public Action<ELogVerbosity, object?>? Logger { get; init; }
	
	private readonly struct ReturnNotNull
	{
		public static ReturnNotNull True => default;
		public static ReturnNotNull? False => null;
	}

	private IInitializingRepository AllocateRepository(IRegistry registry, Type entityType)
	{
		EntityMetadata.Get(entityType, out var metadata);
		Type primaryKeyType = metadata.PrimaryKeyType;
		Type repositoryType = typeof(Repository<,>).MakeGenericType(primaryKeyType, entityType);
		var repository = (IInitializingRepository?)Activator.CreateInstance(repositoryType);
		if (repository is null)
		{
			throw new InvalidOperationException();
		}

		repositoryType.GetProperty(nameof(IRegistryElement.Registry))!.SetValue(repository, registry);
		repositoryType.GetProperty(nameof(IRegistryElement.Name))!.SetValue(repository, $"{entityType.Name}Repository");
		
		(repository as INotifyInitialization)?.PreInitialize();

		return repository;
	}

	private object MakePrimaryKey(in EntityMetadata metadata, ReadOnlySpan<string> rawComponents)
	{
		int32 count = metadata.PrimaryKeyComponents.Count;
		var components = new object[count];
		int32 i = 0;
		foreach (var property in metadata.PrimaryKeyComponents)
		{
			object value = SerializePrimitive(property.PropertyType, rawComponents[i]);
			components[i++] = value;
		}

		return components.Length > 1 ? Activator.CreateInstance(metadata.PrimaryKeyType, components)! : components[0];
	}

	private object? Serialize(Type type, XElement propertyElement, ReturnNotNull? returnNotNullIfNotContainer, GetEntityDelegate getEntity)
	{
		if (type.GetGenericInstanceOf(typeof(IReadOnlyList<>)) is {} genericListType)
		{
			Type elementType = genericListType.GetGenericArguments()[0];
			Type instancedListType = typeof(List<>).MakeGenericType(elementType);
			var container = (IList)Activator.CreateInstance(instancedListType)!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}
				
				object value = SerializeNonContainer(elementType, element, ReturnNotNull.True, getEntity);
				container.Add(value);
			}

			return container;
		}
		else if (type.GetGenericInstanceOf(typeof(IReadOnlySet<>)) is {} genericSetType)
		{
			Type elementType = genericSetType.GetGenericArguments()[0];
			Type instancedSetType = typeof(HashSet<>).MakeGenericType(elementType);
			object container = Activator.CreateInstance(instancedSetType)!;
			MethodInfo addMethod = instancedSetType.GetMethod(nameof(HashSet<>.Add))!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}

				object value = SerializeNonContainer(elementType, element, ReturnNotNull.True, getEntity);
				addMethod.Invoke(container, [ value ]);
			}

			return container;
		}
		else if (type.GetGenericInstanceOf(typeof(IReadOnlyDictionary<,>)) is {} genericMapType)
		{
			Type keyType = genericMapType.GetGenericArguments()[0];
			Type valueType = genericMapType.GetGenericArguments()[1];
			Type instancedMapType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
			var container = (IDictionary)Activator.CreateInstance(instancedMapType)!;
			foreach (var element in propertyElement.Elements())
			{
				if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
				{
					throw new InvalidOperationException();
				}

				string keyString = element.Element(FmlSyntax.MAP_KEY_ELEMENT_NAME)?.Value ?? throw new InvalidOperationException();
				XElement valueElement = element.Element(FmlSyntax.MAP_VALUE_ELEMENT_NAME) ?? throw new InvalidOperationException();
				
				object key = SerializePrimitive(keyType, keyString);
				object value = SerializeNonContainer(valueType, valueElement, ReturnNotNull.True, getEntity);
				container[key] = value;
			}

			return container;
		}
		else if (Nullable.GetUnderlyingType(type) is {} underlyingValueType)
		{
			return SerializeNonContainer(underlyingValueType, propertyElement, ReturnNotNull.False, getEntity);
		}

		return SerializeNonContainer(type, propertyElement, returnNotNullIfNotContainer, getEntity);
	}

	[return: NotNullIfNotNull(nameof(returnNotNull))]
	private object? SerializeNonContainer(Type type, XElement propertyElement, ReturnNotNull? returnNotNull, GetEntityDelegate getEntity)
	{
		bool notnull = returnNotNull is not null;
		bool empty = string.IsNullOrEmpty(propertyElement.Value);
		if (type.IsEnum)
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			return Enum.Parse(type, propertyElement.Value);
		}
		else if (type.IsAssignableTo(typeof(IStruct)))
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			XElement structElement = propertyElement.Elements().Single();
			Type implementationType = SchemaHelper.GetImplementationType(type, structElement.Name.ToString());
			object? instance = Activator.CreateInstance(implementationType);
			if (instance is null)
			{
				throw new InvalidOperationException();
			}
			
			StructMetadata.Get(implementationType, out var metadata);
			foreach (var property in metadata.Properties)
			{
				Type propertyType = property.PropertyType;
				XElement? innerPropertyElement = structElement.Element(property.Name);
				if (innerPropertyElement is null)
				{
					throw new InvalidOperationException();
				}
				
				object? value = Serialize(propertyType, innerPropertyElement, property.IsNotNull() ? ReturnNotNull.True : ReturnNotNull.False, getEntity);
				property.SetValue(instance, value);
			}
			
			return instance;
		}
		else if (type.IsAssignableTo(typeof(IEntity)))
		{
			if (empty)
			{
				return notnull ? throw new InvalidOperationException() : null;
			}
			
			XElement entityReferenceElement = propertyElement.Elements().Single();
			Type implementationType = SchemaHelper.GetImplementationType(type, entityReferenceElement.Name.ToString());
			EntityMetadata.Get(implementationType, out var metadata);
			string[] rawComponents = entityReferenceElement.Value.Split(entityReferenceElement?.Attribute(FmlSyntax.SEP_ATTRIBUTE_NAME)?.Value ?? FmlSyntax.DEFAULT_REFERENCE_SEP);
			object primaryKey = MakePrimaryKey(metadata, rawComponents);
			IEntity? entity = getEntity(implementationType, primaryKey);
			if (notnull && entity is null)
			{
				throw new InvalidOperationException();
			}

			return entity;
		}

		if (empty && !notnull)
		{
			return null;
		}

		return SerializePrimitive(type, propertyElement.Value);
	}

	private object SerializePrimitive(Type type, string value)
	{
		if (!PrimitiveSerializerMap.TryGetValue(type, out var serializer))
		{
			serializer = _fallbackPrimitiveSerializerMap[type];
		}

		return serializer(value);
	}

	private static readonly IReadOnlyDictionary<Type, Func<string, object>> _fallbackPrimitiveSerializerMap = new Dictionary<Type, Func<string, object>>
	{
		[typeof(uint8)] = static value => !string.IsNullOrWhiteSpace(value) ? uint8.Parse(value) : 0,
		[typeof(uint16)] = static value => !string.IsNullOrWhiteSpace(value) ? uint16.Parse(value) : 0,
		[typeof(uint32)] = static value => !string.IsNullOrWhiteSpace(value) ? uint32.Parse(value) : 0,
		[typeof(uint64)] = static value => !string.IsNullOrWhiteSpace(value) ? uint64.Parse(value) : 0,
		[typeof(int8)] = static value => !string.IsNullOrWhiteSpace(value) ? int8.Parse(value) : 0,
		[typeof(int16)] = static value => !string.IsNullOrWhiteSpace(value) ? int16.Parse(value) : 0,
		[typeof(int32)] = static value => !string.IsNullOrWhiteSpace(value) ? int32.Parse(value) : 0,
		[typeof(int64)] = static value => !string.IsNullOrWhiteSpace(value) ? int64.Parse(value) : 0,
		[typeof(float)] = static value => !string.IsNullOrWhiteSpace(value) ? float.Parse(value) : 0,
		[typeof(double)] = static value => !string.IsNullOrWhiteSpace(value) ? double.Parse(value) : 0,
		[typeof(bool)] = static value => !string.IsNullOrWhiteSpace(value) && bool.Parse(value),
		[typeof(string)] = static value => value,
	};
	
}


