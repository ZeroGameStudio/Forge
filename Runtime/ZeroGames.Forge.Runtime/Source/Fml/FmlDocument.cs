// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public readonly record struct FmlDocument(Type RegistryType, XDocument Document);


