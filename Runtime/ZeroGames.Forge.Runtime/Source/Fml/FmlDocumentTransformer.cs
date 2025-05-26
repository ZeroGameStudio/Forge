// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentTransformer(IFmlDocumentSource source, params IEnumerable<IFmltDocumentSource> transformers) : IFmlDocumentSource
{

    [field: MaybeNull]
    public XDocument Document => field ??= Transform();

    private XDocument Transform()
    {
        XDocument result = new(_source.Document);
        
        // @TODO

        return result;
    }

    private readonly IFmlDocumentSource _source = source;
    private readonly IEnumerable<IFmltDocumentSource> _transformers = transformers;
    
}


