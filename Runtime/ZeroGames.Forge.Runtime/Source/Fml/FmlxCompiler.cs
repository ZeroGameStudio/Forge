// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxCompiler
{
    
    public ForgeDocument Compile(ForgeDocument source)
    {
        XDocument result = new(source.FmlDocument);

        foreach (var repositoryElement in result.Root!.Elements())
        {
            string repositoryName = repositoryElement.Name.ToString();
            if (!repositoryName.EndsWith(REPOSITORY_SUFFIX))
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            string entityTypeName = repositoryName.Substring(0, repositoryName.Length - REPOSITORY_SUFFIX.Length);
            if (SchemaHelper.GetDataType(source.RegistryType, entityTypeName) is not { } entityType)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            foreach (var entityElement in repositoryElement.Elements())
            {
                if (entityElement.Name != entityTypeName)
                {
                    throw new InvalidOperationException($"Entity element name {entityElement.Name} mismatch to required {entityType.Name}.");
                }
                
                CompileEntity(entityType, entityElement);
            }
        }

        return source with { FmlDocument = result };
    }

    private void CompileEntity(Type entityType, XElement entityElement)
    {
        EntityMetadata.Get(entityType, out var metadata);

        // 1. Map key as attribute.
        foreach (var property in metadata.RemainingProperties)
        {
            if (property.PropertyType.IsAssignableToSomeGenericInstanceOf(typeof(IReadOnlyDictionary<,>)))
            {
                string propertyName = property.Name;
                if (entityElement.Element(propertyName) is { } propertyElement)
                {
                    foreach (var element in propertyElement.Elements())
                    {
                        if (element.Attribute(MAP_KEY_ELEMENT_NAME) is { } keyAttribute)
                        {
                            string key = keyAttribute.Value;
                            XElement keyElement = new(MAP_KEY_ELEMENT_NAME, key);
                            
                            element.SetAttributeValue(MAP_KEY_ELEMENT_NAME, null);

                            XElement valueElement = new XElement(element) { Name = MAP_VALUE_ELEMENT_NAME };

                            element.RemoveNodes();
                            element.Add(keyElement, valueElement);
                        }
                    }
                }
            }
        }
        
        // 2. Untyped reference/struct.
        
        
        /*
         * TODO: Compile fmlx document object to fml document object.
         * 3. Serialized struct.
         * 4. Serialized container.
         */
    }
    
    private const string REPOSITORY_SUFFIX = "Repository";
    
    private const string MAP_KEY_ELEMENT_NAME = "Key";
    private const string MAP_VALUE_ELEMENT_NAME = "Value";

}


