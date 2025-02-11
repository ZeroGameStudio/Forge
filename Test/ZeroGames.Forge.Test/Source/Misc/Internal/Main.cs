


using System.Xml.Linq;
using ZeroGames.Forge.Runtime;
using ZeroGames.Forge.Test;
using ZeroGames.Forge.Test.Shared;

var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(IXDocumentProvider.Create(XDocument.Load("sharedconfig.xml")), []);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(IXDocumentProvider.Create(XDocument.Load("mainconfig.xml")), [ sharedRegistry ]);
Console.ReadKey();
Console.WriteLine("exit...");


