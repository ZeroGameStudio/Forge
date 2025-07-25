﻿// Copyright Zero Games. All Rights Reserved.

using System.Xml.Linq;

namespace ZeroGames.Forge.Compiler.Frontend.Xml;

public sealed partial class XmlCompilerFrontend : ICompilerFrontend
{

	public XmlCompilerFrontend(XmlCompilerFrontendOptions options)
	{
		_options = options;
	}
	
	public Task<IEnumerable<SchemaSourceUri>> GetImportsAsync(Stream source)
	{
		XElement root = GetRootElement(source);
		try
		{
			IEnumerable<SchemaSourceUri> result = root
				.Elements(IMPORT_ELEMENT_NAME)
				.Select(e => new SchemaSourceUri(e.Attribute(URI_ATTRIBUTE_NAME)!.Value))
				.ToArray();
			
			return Task.FromResult(result);
		}
		catch (Exception ex)
		{
			throw new ParserException(ex);
		}
	}

	public Task<ISchema> CompileAsync(SchemaSourceUri uri, Stream source)
		=> Task.FromResult(ParseSchema(uri, GetRootElement(source)));

	public ICompilationContext CompilationContext { get; private set; } = null!;
	ICompilationContext ICompilationContextReceiver.CompilationContext { set => CompilationContext = value; }

	private XElement GetRootElement(Stream source)
	{
		XDocument doc;
		try
		{
			doc = XDocument.Load(source);
			source.Seek(0, SeekOrigin.Begin);
		}
		catch (Exception ex)
		{
			throw new ParserException(ex);
		}

		XElement? root = doc.Root;
		if (root is null || root.Name != SCHEMA_ELEMENT_NAME)
		{
			throw new ParserException();
		}
		
		return root;
	}

	private ISchema ParseSchema(SchemaSourceUri uri, XElement root)
	{
		IEnumerable<(SchemaSourceUri Uri, string Alias)> imports = root
			.Elements(IMPORT_ELEMENT_NAME)
			.Select(e =>
			{
				SchemaSourceUri import = new(e.Attribute(URI_ATTRIBUTE_NAME)!.Value);
				return (import, e.Attribute(ALIAS_ATTRIBUTE_NAME)?.Value ?? CompilationContext.GetSchema(import).Name);
			})
			.ToArray();
		
		Schema schema = new(imports.ToDictionary(import => import.Alias, import => CompilationContext.GetSchema(import.Uri)), schema => ParseDataTypes(root, schema), schema => ParseMetadatas(root, schema))
		{
			Uri = uri,
			Name = GetName(root),
			Namespace = GetNamespace(root),
		};

		foreach (var type in schema.DataTypes.OfType<ICompositeDataType>().Select(x => (ISetupDependenciesSource)x))
		{
			if (!type.SetupDependencies())
			{
				throw new ParserException();
			}
			
			foreach (var prop in ((ICompositeDataType)type).Properties.Select(x => (ISetupDependenciesSource)x))
			{
				if (!prop.SetupDependencies())
				{
					throw new ParserException();
				}
			}
		}
		
		ValidateSchema(schema);
		
		return schema;
	}

	private void ValidateSchema(ISchema schema)
	{
		static void Gather(ICompositeDataType dataType, HashSet<string> allPropertyNames, HashSet<string> allTypeNames)
		{
			allPropertyNames.UnionWith(dataType.Properties.Select(property => property.Name));
			allTypeNames.Add(dataType.Name);

			if (dataType.BaseType is { } baseType)
			{
				Gather(baseType, allPropertyNames, allTypeNames);
			}

			foreach (var i in dataType.Interfaces)
			{
				Gather(i, allPropertyNames, allTypeNames);
			}
		}
		
		foreach (var compositeType in schema.DataTypes.OfType<ICompositeDataType>())
		{
			HashSet<string> allPropertyNames = [];
			HashSet<string> allTypeNames = [];
			Gather(compositeType, allPropertyNames, allTypeNames);

			if (allPropertyNames.Overlaps(allTypeNames))
			{
				throw new ParserException("One or more property names are conflict with type name.");
			}
		}
	}

	private readonly XmlCompilerFrontendOptions _options;

}


