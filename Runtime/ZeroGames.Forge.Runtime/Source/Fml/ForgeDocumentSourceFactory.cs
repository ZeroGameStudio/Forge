// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class ForgeDocumentSourceFactory(Type registryType)
{
    public Type RegistryType { get; } = registryType;
}

public class ForgeDocumentSourceFactory<T>() : ForgeDocumentSourceFactory(typeof(T)) where T : class, IRegistry;

public static class ForgeDocumentSourceFactoryExtensions
{
    public static FmlxDocumentSource CreateFmlx(this ForgeDocumentSourceFactory factory, XDocument source) => new(factory.RegistryType, source);
    public static FmlxDocumentSource CreateFmlx(this ForgeDocumentSourceFactory factory, Stream source) => new(factory.RegistryType, source);
    public static FmlxDocumentSource CreateFmlx(this ForgeDocumentSourceFactory factory, string sourcePath) => new(factory.RegistryType, XDocument.Load(sourcePath));
    
    public static FmlDocumentSource CreateFml(this ForgeDocumentSourceFactory factory, XDocument source) => new(factory.RegistryType, source);
    public static FmlDocumentSource CreateFml(this ForgeDocumentSourceFactory factory, Stream source) => new(factory.RegistryType, source);
    public static FmlDocumentSource CreateFml(this ForgeDocumentSourceFactory factory, string sourcePath) => new(factory.RegistryType, XDocument.Load(sourcePath));
}


