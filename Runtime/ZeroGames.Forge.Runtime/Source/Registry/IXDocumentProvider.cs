// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Runtime;

public interface IXDocumentProvider
{
	public static IXDocumentProvider FromFile(string filepath) => new XDocumentObjectProvider(XDocument.Load(filepath));
	public static IXDocumentProvider[] FromFiles(params IEnumerable<string> filepaths) => filepaths.Select(FromFile).ToArray();
	
	public XDocument Document { get; }
	// @TODO: bool AllowMergeRepository
	// @TODO: bool AllowOverrideEntity
}


