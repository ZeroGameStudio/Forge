﻿// Copyright Zero Games. All Rights Reserved.

using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class RegistryFactory : IRegistryFactory
{

	public object Create(IFmlDocumentSource source, params IEnumerable<IRegistry> imports)
	{
		Type registryType = source.Document.RegistryType;
		
		Logger?.Invoke(ELogVerbosity.Log, $"Creating {registryType.Name}...");
		
		if (registryType.IsInterface || registryType.IsAbstract || !registryType.IsAssignableTo(typeof(IRegistry)))
		{
			throw new ArgumentOutOfRangeException(nameof(registryType));
		}

		XDocument document = source.Document.Document;
		if (document.Root?.Name != registryType.Name)
		{
			throw new InvalidOperationException($"Document root does not match registry type {registryType.Name}.");
		}
		
		Dictionary<string, IRegistry> importMap = imports.ToDictionary(import => import.Name);

		var registry = (IRegistry)Activator.CreateInstance(registryType)!;
		(registry as INotifyInitialization)?.PreInitialize();

		RegistryMetadata.Get(registryType, out var metadata);
		
		Dictionary<Type, IRepository> repositoryByEntityType = [];
		{ // Stage I: Fill import registries
			Logger?.Invoke(ELogVerbosity.Log, "Importing dependencies...");
			foreach (var importProperty in metadata.Imports)
			{
				Logger?.Invoke(ELogVerbosity.Log, $"Importing dependency {importProperty.Name}...");
				
				Type propertyType = importProperty.PropertyType;
				string name = propertyType.Name;
				if (!importMap.TryGetValue(name, out var import))
				{
					throw new KeyNotFoundException($"Import registry '{name}' not found.");
				}

				Type importType = import.GetType();
				if (!importType.IsAssignableTo(propertyType))
				{
					throw new ArgumentException($"Import registry '{name}' is not assignable to property with type '{propertyType}'.");
				}
			
				importProperty.SetValue(registry, import);
				
				RegistryMetadata.Get(importType, out var importMetadata);
				foreach (var repositoryProperty in importMetadata.Repositories)
				{
					Type repositoryType = repositoryProperty.PropertyType;
					Type entityType = repositoryType.GetGenericInstanceOf(typeof(IRepository<,>))!.GetGenericArguments()[1];
					repositoryByEntityType[entityType] = (IRepository)repositoryProperty.GetValue(import)!;
				}
			}
		}
		
		RepositoryFactory factory = new()
		{
			PrimitiveSerializerMap = new Dictionary<Type, Func<string, object>>(),
			Logger = Logger,
		};
		var finishInitializations = new RepositoryFactory.FinishInitializationDelegate[metadata.Repositories.Count];
		{ // Stage II: Allocate all repositories first but not initialize here (only properties defined by IEntity interface is available on entities).
			Logger?.Invoke(ELogVerbosity.Log, "Allocating repositories...");
			int32 i = 0;
			foreach (var repositoryProperty in metadata.Repositories)
			{
				Logger?.Invoke(ELogVerbosity.Log, $"Allocating {repositoryProperty.Name}...");
				
				Type propertyType = repositoryProperty.PropertyType;
				Type entityType = propertyType.GetGenericInstanceOf(typeof(IRepository<,>))!.GetGenericArguments()[1];
				XElement? root = document.Root?.Element($"{entityType.Name}Repository");
				if (root is null ^ entityType.IsAbstract)
				{
					throw new InvalidOperationException($"Missing repository {entityType.Name}.");
				}

				IInitializingRepository repository = root is not null ? factory.Create(registry, entityType, root, out var finishInitialization) : factory.Create(registry, entityType, out finishInitialization);
				if (!repository.GetType().IsAssignableTo(propertyType))
				{
					throw new InvalidOperationException($"Repository type {entityType.Name} mismatch to required type {propertyType.Name}.");
				}
			
				repositoryProperty.SetValue(registry, repository);

				finishInitializations[i++] = finishInitialization;
				repositoryByEntityType[entityType] = repository;
			}
		}
		
		{ // Stage III: Merge concrete entities in inherited repository into base repository.
			Logger?.Invoke(ELogVerbosity.Log, "Merging repositories to base...");
			foreach (var repository in repositoryByEntityType
				         .Select(pair => pair.Value)
				         .Where(repo => repo.EntityType.BaseType is {} baseType && baseType != typeof(object))
				         .OrderByDescending(repo =>
				         {
					         int32 depth = 0;
					         for (Type? baseType = repo.EntityType.BaseType; baseType is {} b && b != typeof(object); baseType = baseType.BaseType) ++depth;
					         return depth;
				         }))
			{
				var baseRepository = (IInitializingRepository)repositoryByEntityType[repository.EntityType.BaseType!];
				
				Logger?.Invoke(ELogVerbosity.Log, $"Merging {repository.Name} to {baseRepository.Name}...");
				
				foreach (var entity in repository.Entities)
				{
					baseRepository.RegisterEntity(entity, false);
				}
			}
		}
		
		{ // Stage IV: Now all entities are in right place, and we can initialize them: Fill data, fixup references, etc.
			Logger?.Invoke(ELogVerbosity.Log, "Fixing up registry...");
			RepositoryFactory.GetEntityDelegate getEntity = (type, primaryKey) => repositoryByEntityType[type][primaryKey];
			foreach (var finishInitialization in finishInitializations)
			{
				finishInitialization(getEntity);
			}
		}

		{ // Stage V: Build indices.
			foreach (var autoIndex in metadata.AutoIndices)
			{
				throw new NotImplementedException();
			}
		}
		
		(registry as INotifyInitialization)?.PostInitialize();
		
		Logger?.Invoke(ELogVerbosity.Log, $"Successfully created {registry.Name}!");
		
		return registry;
	}
	
	public Action<ELogVerbosity, object?>? Logger { get; init; }

}


