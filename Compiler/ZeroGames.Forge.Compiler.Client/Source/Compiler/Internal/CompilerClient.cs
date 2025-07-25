﻿// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics;
using System.Text.Json;
using ZeroGames.Forge.Compiler.Backend;
using ZeroGames.Forge.Compiler.Backend.CSharp;
using ZeroGames.Forge.Compiler.Frontend;
using ZeroGames.Forge.Compiler.Frontend.Xml;
using ZeroGames.Forge.Compiler.Server;

namespace ZeroGames.Forge.Compiler.Client;

internal sealed class CompilerClient
{

	public enum ELogLevel : uint8
	{
		Log = 0,
		Warning = 1,
		Error = 2,
	}

	public CompilerClient(string sourceDirs, string sources, string outputDir, string configOverride, Action<ELogLevel, object> logger)
	{
		_sourceDirs = sourceDirs.Split(';');
		_sources = sources.Split(';').Select(source => new SchemaSourceUri(source)).ToHashSet();
		_outputDir = outputDir;
		_configOverride = configOverride;
		_logger = logger;
		
		ParseConfig(out _config);
	}

	public bool Compile() => CompileAsync().Result;

	private void ParseConfig(out CompilerClientConfig config)
	{
		string configPath = !string.IsNullOrWhiteSpace(_configOverride) ? _configOverride : "./config.json";
		if (!File.Exists(configPath))
		{
			throw new FileNotFoundException($"Config file {configPath} not found.");
		}
		
		using FileStream fs = File.OpenRead(configPath);
		config = JsonSerializer.Deserialize<CompilerClientConfig>(fs, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		});
	}

	private ICompiler CreateServer()
	{
		CompilerOptions options = new()
		{
			SchemaSourceResolver = CreateSchemaSourceResolver(),
			Frontend = CreateFrontend(),
			Backend = CreateBackend(),
			Primitives = CreatePrimitives(),
			GenericListType = CreateGenericContainerDataType(_config.ListTypeName, EContainerType.List),
			GenericSetType = CreateGenericContainerDataType(_config.SetTypeName, EContainerType.Set),
			GenericMapType = CreateGenericContainerDataType(_config.MapTypeName, EContainerType.Map),
			GenericOptionalType = CreateGenericContainerDataType(_config.OptionalTypeName, EContainerType.Optional),
		};
		return ICompiler.Create(options);
	}

	private ISchemaSourceResolver CreateSchemaSourceResolver()
	{
		return new SchemaSourceResolver(_sourceDirs);
	}

	private IReadOnlyDictionary<SchemaSourceForm, ICompilerFrontend> CreateFrontend()
	{
		return new Dictionary<SchemaSourceForm, ICompilerFrontend>
		{
			[new("xml")] = new XmlCompilerFrontend(new()
			{
				DefaultPrimaryKey = _config.DefaultPrimaryKey,
				DefaultEnumUnderlyingTypeName = _config.DefaultEnumUnderlyingTypeName,
			}),
		};
	}

	private ICompilerBackend CreateBackend()
	{
		return new CSharpCompilerBackend(new()
		{
			ImplicitlyUsings = _config.ImplicitUsings,
			GeneratesPartialTypes = _config.GeneratesPartialTypes,
		});
	}

	private IReadOnlySet<IPrimitiveDataType> CreatePrimitives()
	{
		HashSet<IPrimitiveDataType> result = 
		[
			CreateIntrinsicPrimitiveDataType(_config.UInt8TypeName, EPrimitiveType.UInt8),
			CreateIntrinsicPrimitiveDataType(_config.UInt16TypeName, EPrimitiveType.UInt16),
			CreateIntrinsicPrimitiveDataType(_config.UInt32TypeName, EPrimitiveType.UInt32),
			CreateIntrinsicPrimitiveDataType(_config.UInt64TypeName, EPrimitiveType.UInt64),
			CreateIntrinsicPrimitiveDataType(_config.Int8TypeName, EPrimitiveType.Int8),
			CreateIntrinsicPrimitiveDataType(_config.Int16TypeName, EPrimitiveType.Int16),
			CreateIntrinsicPrimitiveDataType(_config.Int32TypeName, EPrimitiveType.Int32),
			CreateIntrinsicPrimitiveDataType(_config.Int64TypeName, EPrimitiveType.Int64),
			CreateIntrinsicPrimitiveDataType(_config.FloatTypeName, EPrimitiveType.Float),
			CreateIntrinsicPrimitiveDataType(_config.DoubleTypeName, EPrimitiveType.Double),
			CreateIntrinsicPrimitiveDataType(_config.BoolTypeName, EPrimitiveType.Bool),
			CreateIntrinsicPrimitiveDataType(_config.StringTypeName, EPrimitiveType.String),
		];
		
		// @TODO: Custom types

		return result;
	}

	private IPrimitiveDataType CreateIntrinsicPrimitiveDataType(string name, EPrimitiveType type)
		=> new PrimitiveDataType { Name = name, Namespace = string.Empty, PrimitiveType = type, CanBeKey = true };

	private IPrimitiveDataType CreateCustomPrimitiveDataType(string name, string @namespace, bool canBeKey)
		=> new PrimitiveDataType { Name = name, Namespace = @namespace, PrimitiveType = EPrimitiveType.Custom, CanBeKey = canBeKey };

	private IGenericContainerDataType CreateGenericContainerDataType(string name, EContainerType type)
		=> new GenericContainerDataType { Name = name, ContainerType = type };

	private async Task<bool> CompileAsync()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		bool success = true;
		try
		{
			if (Directory.Exists(_outputDir))
			{
				if (_config.RequiresOutputDirNotExists)
				{
					throw new InvalidOperationException();
				}

				Directory.Delete(_outputDir, true);
			}

			Directory.CreateDirectory(_outputDir);
			
			ICompiler server = CreateServer();
			await foreach (var result in server.CompileAsync(_sources))
			{
				ELogLevel logLevel = result.ErrorLevel switch
				{
					ECompilationErrorLevel.Success or ECompilationErrorLevel.Info => ELogLevel.Log,
					ECompilationErrorLevel.Warning => ELogLevel.Warning,
					ECompilationErrorLevel.Error => ELogLevel.Error,
					_ => ELogLevel.Log,
				};
				_logger(logLevel, $"{result.Properties["Uri"]} - {result.Message}");
				if (result.ErrorLevel > ECompilationErrorLevel.Warning)
				{
					success = false;
					continue;
				}

				Stream dest = result.Dest;
				string outputDir = $"{_outputDir}/{result.Properties["Schema"]}";
				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				string outputPath = $"{outputDir}/{result.Properties["Name"]}.cs";
				await using FileStream fs = File.OpenWrite(outputPath);
				await dest.CopyToAsync(fs);
				_logger(ELogLevel.Log, $"File generated: {outputPath}");
			}
		}
		catch (Exception)
		{
			success = false;
			throw;
		}
		finally
		{
			if (!success)
			{
				Directory.Delete(_outputDir, true);
			}
			
			_logger(ELogLevel.Log, $"Compilation finish using {stopwatch.ElapsedMilliseconds}ms");
		}
		
		return success;
	}

	private readonly IReadOnlyList<string> _sourceDirs;
	private readonly IReadOnlySet<SchemaSourceUri> _sources;
	private readonly string _outputDir;
	private readonly string _configOverride;
	private readonly Action<ELogLevel, object> _logger;

	private readonly CompilerClientConfig _config;

}


