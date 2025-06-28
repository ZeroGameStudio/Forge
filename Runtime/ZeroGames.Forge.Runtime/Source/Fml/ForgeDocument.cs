// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public readonly record struct ForgeDocument(Type RegistryType, XDocument FmlDocument);


