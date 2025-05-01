


using System.Xml.Linq;
using ZeroGames.Forge.Runtime;
using ZeroGames.Forge.Test;
using ZeroGames.Forge.Test.Shared;

var sharedRegistry = new RegistryFactory().Create<SharedRegistry>(IXDocumentProvider.FromFiles("sharedconfig.xml"), []);
var mainRegistry = new RegistryFactory().Create<MainRegistry>(IXDocumentProvider.FromFiles("mainconfig.xml", "mainconfig2.xml", "mainconfig_mod.xml"), [ sharedRegistry ]);
Console.ReadKey();
Console.WriteLine("exit...");


