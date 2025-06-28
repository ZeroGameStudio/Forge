// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentAggregator : IFmlDocumentSource
{

    public FmlDocumentAggregator(params IEnumerable<IFmlDocumentSource> sources)
    {
	    _sources = sources.ToList();
	    if (_sources.Count == 0)
	    {
		    throw new ArgumentOutOfRangeException(nameof(sources));
	    }

	    _registryType = _sources[0].Document.RegistryType;
	    if (_registryType.IsAbstract || !_registryType.IsAssignableTo(typeof(IRegistry)))
	    {
		    throw new ArgumentOutOfRangeException(nameof(sources));
	    }
    }

    public FmlDocument Document => field == default ? field = Aggregate() : field;

    public bool AllowsMergeRepository { get; init; } = true;
    public bool AllowsOverrideEntity { get; init; } = true;

    private FmlDocument Aggregate()
    {
        XDocument result = new(new XElement(_registryType.Name));
		foreach (var source in _sources.Select(s => s.Document.Document))
		{
			if (result.Root!.Name != source.Root?.Name)
			{
				throw new InvalidOperationException($"Source root node {source.Root?.Name} mismatch.");
			}

			DataTypesAttribute dataTypesAttribute = _registryType.GetCustomAttribute<SchemaAttribute>()!.Schema.GetCustomAttribute<DataTypesAttribute>()!;
			
			foreach (var repository in source.Root.Elements())
			{
				XElement? existingRepository = result.Root.Elements(repository.Name).SingleOrDefault();
				if (existingRepository is null)
				{
					if (dataTypesAttribute.Types.All(type => type.Name + FmlSyntax.REPOSITORY_SUFFIX != repository.Name))
					{
						throw new InvalidOperationException($"Unknown repository {repository.Name}.");
					}
					
					result.Root.Add(new XElement(repository));
				}
				else
				{
					if (!AllowsMergeRepository)
					{
						throw new InvalidOperationException($"Duplicated repository {repository.Name}.");
					}
					
					string[]? primaryKeyComponents = null;
					foreach (var entity in repository.Elements())
					{
						primaryKeyComponents ??= dataTypesAttribute[entity.Name.ToString()].GetCustomAttribute<PrimaryKeyAttribute>()!.Components.ToArray();
						
						string[] GetRawComponents(XElement element)
							=> element
								.Elements()
								.Where(elem => primaryKeyComponents.Contains(elem.Name.ToString()))
								.OrderBy(elem => primaryKeyComponents.IndexOf(elem.Name.ToString()))
								.Select(elem => elem.Value)
								.ToArray();
						
						string[] rawComponents = GetRawComponents(entity);
						if (rawComponents.Length != primaryKeyComponents.Length)
						{
							throw new InvalidOperationException("Missing primary key.");
						}

						if (existingRepository.Elements().SingleOrDefault(e =>
						    {
							    string[] existingRawComponents = GetRawComponents(e);
							    if (existingRawComponents.Length != rawComponents.Length)
							    {
								    return false;
							    }
							    
							    for (int32 i = 0; i < existingRawComponents.Length; ++i)
							    {
								    if (existingRawComponents[i] != rawComponents[i])
								    {
									    return false;
								    }
							    }

							    return true;
						    }) is {} existingEntity)
						{
							if (!AllowsOverrideEntity)
							{
								throw new InvalidOperationException($"Duplicated entity {{{string.Join(", ", rawComponents)}}}.");
							}

							existingEntity.Remove();
						}
						
						existingRepository.Add(new XElement(entity));
					}
				}
			}
		}

        return new(_registryType, result);
    }
    
    private readonly Type _registryType;
    private readonly List<IFmlDocumentSource> _sources;
    
}


