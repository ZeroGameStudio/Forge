using System.Xml.Linq;
using ZeroGames.Forge.Runtime;
using ZeroGames.Forge.Test;
using ZeroGames.Forge.Test.Shared;

var sharedSource = new FmlxDocumentSource<SharedRegistry>(XDocument.Load("sharedconfig.xml"));
var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(sharedSource);

ForgeDocumentSourceFactory<MainRegistry> factory = new();
var mainSource = new FmlDocumentTransformer
(
    new FmlDocumentAggregator
    (
        factory.FromFmlx("mainconfig.xml"),
        factory.FromFmlx("mainconfig2.xml"),
        factory.FromFmlx("mainconfig_mod.xml")
    )
);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(mainSource, sharedRegistry);

Console.WriteLine("所有测试完成，按任意键退出...");
Console.ReadKey();
Console.WriteLine("exit...");


