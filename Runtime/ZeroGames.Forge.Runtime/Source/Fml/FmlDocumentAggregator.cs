// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentAggregator : IFmlDocumentSource
{

    public FmlDocumentAggregator(Type registryType, params IEnumerable<IFmlDocumentSource> sources)
    {
	    if (registryType.IsAbstract || !registryType.IsAssignableTo(typeof(IRegistry)))
	    {
		    throw new ArgumentOutOfRangeException(nameof(registryType));
	    }
	    
	    _registryType = registryType;
	    _sources = sources;
    }

    [field: MaybeNull]
    public XDocument Document => field ??= Aggregate(_registryType, _sources);

    public bool AllowsMergeRepository { get; init; } = true;
    public bool AllowsOverrideEntity { get; init; } = true;

    private XDocument Aggregate(Type registryType, IEnumerable<IFmlDocumentSource> sources)
    {
        XDocument result = new(new XElement(registryType.Name));
		foreach (var source in sources.Select(s => s.Document))
		{
			if (result.Root!.Name != source.Root?.Name)
			{
				throw new InvalidOperationException($"Source root node {source.Root?.Name} mismatch.");
			}

			DataTypesAttribute dataTypesAttribute = registryType.GetCustomAttribute<SchemaAttribute>()!.Schema.GetCustomAttribute<DataTypesAttribute>()!;
			
			foreach (var repository in source.Root.Elements())
			{
				XElement? existingRepository = result.Root.Elements(repository.Name).SingleOrDefault();
				if (existingRepository is null)
				{
					if (dataTypesAttribute.Types.All(type => type.Name + REPOSITORY_SUFFIX != repository.Name))
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

        return result;
    }

    private const string REPOSITORY_SUFFIX = "Repository";
    
    private readonly Type _registryType;
    private readonly IEnumerable<IFmlDocumentSource> _sources;
    
}

public class FmlDocumentAggregator<T>(params IEnumerable<IFmlDocumentSource> sources) : FmlDocumentAggregator(typeof(T), sources) where T : class, IRegistry;


