﻿// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public interface IFmltDocumentSource
{
    public XDocument Document { get; }
}


