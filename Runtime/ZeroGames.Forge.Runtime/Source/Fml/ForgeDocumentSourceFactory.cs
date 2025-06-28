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
    public static FmlxDocumentSource FromFmlx(this ForgeDocumentSourceFactory factory, XDocument source, bool fmlx = true) => new(factory.RegistryType, source, fmlx);
    public static FmlxDocumentSource FromFmlx(this ForgeDocumentSourceFactory factory, Stream source, bool fmlx = true) => new(factory.RegistryType, source, fmlx);
    public static FmlxDocumentSource FromFmlx(this ForgeDocumentSourceFactory factory, string sourcePath, bool fmlx = true) => new(factory.RegistryType, XDocument.Load(sourcePath), fmlx);
}


