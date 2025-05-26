


using System.Xml.Linq;
using ZeroGames.Forge.Runtime;
using ZeroGames.Forge.Test;
using ZeroGames.Forge.Test.Shared;

var sharedSource = new FmlDocumentSource(XDocument.Load("sharedconfig.xml"));
var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(sharedSource);

var mainSource = new FmlDocumentTransformer
(
    new FmlDocumentAggregator<MainRegistry>
    (
        new FmlDocumentSource(XDocument.Load("mainconfig.xml")),
        new FmlDocumentSource(XDocument.Load("mainconfig2.xml")),
        new FmlDocumentSource(XDocument.Load("mainconfig_mod.xml"))
    )
);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(mainSource, sharedRegistry);

Console.ReadKey();
Console.WriteLine("exit...");


