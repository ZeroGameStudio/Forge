// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public class FmlDocumentTransformer(IForgeDocumentSource source, params IEnumerable<IFmltDocumentSource> transformers) : IForgeDocumentSource
{

    public ForgeDocument Document => field == default ? field = Transform() : field;

    private ForgeDocument Transform()
    {
        XDocument result = new(_source.Document.FmlDocument);
        
        // @TODO: Apply transforms.

        return _source.Document with { FmlDocument = result };
    }

    private readonly IForgeDocumentSource _source = source;
    private readonly IEnumerable<IFmltDocumentSource> _transformers = transformers;
    
}


