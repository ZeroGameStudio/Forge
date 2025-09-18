// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlxCompiler
{

    public IReadOnlySet<string>? AllowedEntityElementPlaceholders { get; init; }
    
    public FmlDocument Compile(FmlDocument source)
    {
        XDocument result = new(source.Document);

        foreach (var repositoryElement in result.Root!.Elements())
        {
            string repositoryName = repositoryElement.Name.ToString();
            if (!repositoryName.EndsWith(FmlSyntax.REPOSITORY_SUFFIX))
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            string entityTypeName = repositoryName.Substring(0, repositoryName.Length - FmlSyntax.REPOSITORY_SUFFIX.Length);
            if (SchemaHelper.GetDataType(source.RegistryType, entityTypeName) is not { } entityType)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            EntityMetadata? metadata = null;
            foreach (var entityElement in repositoryElement.Elements())
            {
                if (entityElement.Name != entityTypeName)
                {
                    if (AllowedEntityElementPlaceholders?.Contains(entityElement.Name.ToString()) is not true)
                    {
                        throw new InvalidOperationException($"Entity element name {entityElement.Name} mismatch to required {entityType.Name}.");
                    }

                    entityElement.Name = entityTypeName;
                }

                if (metadata is null)
                {
                    EntityMetadata.Get(entityType, out var meta);
                    metadata = meta;
                }
                
                CompileEntity(metadata, entityElement);
            }
        }

        return source with { Document = result };
    }

    private void CompileEntity(EntityMetadata metadata, XElement entityElement)
    {
        // IMPORTANT: DO NOT CHANGE ORDER!
        DecayMapKeyAndAddExplicitPropertyType(metadata, entityElement);
        
        // TODO: Serialized struct/container.
    }

    private void DecayMapKeyAndAddExplicitPropertyType(ICompositeDataTypeMetadata metadata, XElement dataElement)
    {
        // Modify the map <Element> element so that it has <Key> and <Value> elements under it without Key attribute.
        static void EnsureMapKeyDecayed(XElement element)
        {
            if (element.Attribute(FmlSyntax.MAP_KEY_ELEMENT_NAME) is { } keyAttribute)
            {
                string key = keyAttribute.Value;
                XElement keyElement = new(FmlSyntax.MAP_KEY_ELEMENT_NAME, key);
                            
                element.SetAttributeValue(FmlSyntax.MAP_KEY_ELEMENT_NAME, null);

                XElement valueElement = new XElement(element) { Name = FmlSyntax.MAP_VALUE_ELEMENT_NAME };

                element.RemoveNodes();
                element.Add(keyElement, valueElement);
            }
        }
        
        // Modify the reference/struct <SomeProperty> element so that it has an explicit type element under it.
        static void EnsurePropertyTyped(XElement propertyElement, Type propertyType, out Type? actualType)
        {
            actualType = propertyType;
            
            bool untyped = true;
            if (propertyType.IsAssignableTo(typeof(IEntity)))
            {
                if (propertyElement.HasElements)
                {
                    untyped = false;
                }
                else if (string.IsNullOrEmpty(propertyElement.Value))
                {
                    untyped = false;
                    actualType = null;
                }
            }
            else if (propertyType.IsAssignableTo(typeof(IStruct)))
            {
                if (propertyType.IsAbstract)
                {
                    throw new InvalidOperationException("Cannot use untyped struct for abstract property.");
                }

                if (!propertyElement.HasElements)
                {
                    untyped = false;
                    actualType = null;
                }
                else if (propertyElement.Elements().Count() == 1 && SchemaHelper.GetDataType(propertyType, propertyElement.Elements().Single().Name.ToString()) is { } maybeType && maybeType.IsAssignableTo(propertyType))
                {
                    untyped = false;
                    actualType = maybeType;
                }
            }
            else
            {
                throw new InvalidOperationException("Impossible code path.");
            }

            if (untyped)
            {
                XElement typedElement = new XElement(propertyElement) { Name = propertyType.Name };
                propertyElement.RemoveNodes();
                propertyElement.Add(typedElement);
            }
        }

        static void ConditionallyRecurseStruct(FmlxCompiler @this, XElement propertyElement, Type? structType)
        {
            if (structType is null || !structType.IsAssignableTo(typeof(IStruct)))
            {
                return;
            }
            
            XElement structElement = propertyElement.Elements().Single();
            StructMetadata.Get(structType, out var structMetadata);
            @this.DecayMapKeyAndAddExplicitPropertyType(structMetadata, structElement);
        }
        
        foreach (var property in metadata.MapProperties.Concat(metadata.CompositeProperties).Distinct())
        {
            Type propertyType = property.PropertyType;
            string propertyName = property.Name;
            if (dataElement.Element(propertyName) is { } propertyElement)
            {
                if (propertyType.IsAssignableTo(typeof(IEntity)))
                {
                    EnsurePropertyTyped(propertyElement, propertyType, out _);
                }
                else if (propertyType.IsAssignableTo(typeof(IStruct)))
                {
                    EnsurePropertyTyped(propertyElement, propertyType, out var structType);
                    ConditionallyRecurseStruct(this, propertyElement, structType);
                }
                else if (propertyType.GetGenericInstanceOf(typeof(IReadOnlyList<>)) is { } genericListType)
                {
                    foreach (var element in propertyElement.Elements())
                    {
                        if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
                        {
                            throw new InvalidOperationException($"Unexpected element {element.Name} in container.");
                        }

                        EnsurePropertyTyped(element, genericListType.GetGenericArguments()[0], out var maybeStructType);
                        ConditionallyRecurseStruct(this, element, maybeStructType);
                    }
                }
                else if (propertyType.GetGenericInstanceOf(typeof(IReadOnlySet<>)) is { } genericSetType)
                {
                    foreach (var element in propertyElement.Elements())
                    {
                        if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
                        {
                            throw new InvalidOperationException($"Unexpected element {element.Name} in container.");
                        }

                        EnsurePropertyTyped(element, genericSetType.GetGenericArguments()[0], out var maybeStructType);
                        ConditionallyRecurseStruct(this, element, maybeStructType);
                    }
                }
                else if (propertyType.GetGenericInstanceOf(typeof(IReadOnlyDictionary<,>)) is { } genericMapType)
                {
                    // Map has been flattened to standard FML syntax so we can get <Value> element.
                    foreach (var element in propertyElement.Elements())
                    {
                        if (element.Name != FmlSyntax.CONTAINER_ELEMENT_ELEMENT_NAME)
                        {
                            throw new InvalidOperationException($"Unexpected element {element.Name} in container.");
                        }
                        
                        // Ensure key attribute is decayed first so it must have <Value> element under it.
                        EnsureMapKeyDecayed(element);
                        
                        // Note that we concat all map properties so it may not be a struct map.
                        Type valueType = genericMapType.GetGenericArguments()[1];
                        if (valueType.IsAssignableTo(typeof(IEntity)) || valueType.IsAssignableTo(typeof(IStruct)))
                        {
                            XElement valueElement = element.Element(FmlSyntax.MAP_VALUE_ELEMENT_NAME)!;
                            EnsurePropertyTyped(valueElement, valueType, out var maybeStructType);
                            ConditionallyRecurseStruct(this, valueElement, maybeStructType);
                        }
                    }
                }
            }
        }
    }

}


