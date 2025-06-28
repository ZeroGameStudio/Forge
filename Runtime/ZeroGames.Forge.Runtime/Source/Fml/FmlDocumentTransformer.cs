// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentTransformer(IFmlDocumentSource source, params IEnumerable<IFmltDocumentSource> transformers) : IFmlDocumentSource
{

    public FmlDocument Document => field == default ? field = Transform() : field;

    private FmlDocument Transform()
    {
        XDocument result = new(_source.Document.Document);
        
        // @TODO: Apply transforms.

        return _source.Document with { Document = result };
    }

    private readonly IFmlDocumentSource _source = source;
    private readonly IEnumerable<IFmltDocumentSource> _transformers = transformers;
    
}


