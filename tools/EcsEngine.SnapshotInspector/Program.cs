using EcsEngine.Replay;

if (args.Length == 0)
{
	PrintUsage();
	return 1;
}

string? input = null;
string format = "text";
string? output = null;
List<string> schemaArgs = [];

for (int i = 0; i < args.Length; i++)
{
	string arg = args[i];
	switch (arg)
	{
		case "--format":
			if (i + 1 >= args.Length)
			{
				Console.Error.WriteLine("Missing value for --format.");
				return 2;
			}

			format = args[++i].Trim().ToLowerInvariant();
			break;

		case "--output":
			if (i + 1 >= args.Length)
			{
				Console.Error.WriteLine("Missing value for --output.");
				return 2;
			}

			output = args[++i];
			break;

		case "--schema":
			if (i + 1 >= args.Length)
			{
				Console.Error.WriteLine("Missing value for --schema.");
				return 2;
			}

			schemaArgs.Add(args[++i]);
			break;

		case "--help":
		case "-h":
			PrintUsage();
			return 0;

		default:
			if (arg.StartsWith("--", StringComparison.Ordinal))
			{
				Console.Error.WriteLine($"Unknown argument: {arg}");
				return 2;
			}

			if (input is not null)
			{
				Console.Error.WriteLine("Only one input snapshot path is supported.");
				return 2;
			}

			input = arg;
			break;
	}
}

if (string.IsNullOrWhiteSpace(input))
{
	Console.Error.WriteLine("Input snapshot path is required.");
	PrintUsage();
	return 2;
}

if (!File.Exists(input))
{
	Console.Error.WriteLine($"Snapshot file not found: {input}");
	return 2;
}

if (format is not ("text" or "json"))
{
	Console.Error.WriteLine("--format must be either 'text' or 'json'.");
	return 2;
}

try
{
	List<SnapshotComponentSchema> schemas = [.. schemaArgs.Select(ParseSchemaArg)];
	SnapshotInspector inspector = new(schemas);

	using FileStream fs = File.OpenRead(input);
	using BinaryReader reader = new(fs);
	SnapshotInspectionResult result = inspector.Inspect(reader);

	string rendered = format == "json"
		? SnapshotInspector.ToJson(result)
		: SnapshotInspector.ToText(result);

	if (string.IsNullOrWhiteSpace(output))
	{
		Console.WriteLine(rendered);
	}
	else
	{
		File.WriteAllText(output, rendered);
		Console.WriteLine($"Wrote {format} output to: {output}");
	}

	return 0;
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
	return 1;
}

static SnapshotComponentSchema ParseSchemaArg(string value)
{
	int equalsIndex = value.IndexOf('=');
	if (equalsIndex <= 0 || equalsIndex == value.Length - 1)
	{
		throw new ArgumentException(
			"Invalid --schema value. Expected format: TypeName=field:type,field:type");
	}

	string typeName = value[..equalsIndex].Trim();
	string payload = value[(equalsIndex + 1)..].Trim();

	string[] fieldTokens = payload.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	if (fieldTokens.Length == 0)
		throw new ArgumentException($"Schema for '{typeName}' must define at least one field.");

	List<SnapshotComponentField> fields = new(fieldTokens.Length);
	foreach (string token in fieldTokens)
	{
		string[] parts = token.Split(':', StringSplitOptions.TrimEntries);
		if (parts.Length != 2)
		{
			throw new ArgumentException(
				$"Invalid field token '{token}' in schema '{typeName}'. Expected field:type.");
		}

		fields.Add(new SnapshotComponentField(parts[0], ParseFieldType(parts[1])));
	}

	return new SnapshotComponentSchema(typeName, fields);
}

static SnapshotFieldType ParseFieldType(string raw)
{
	return raw.Trim().ToLowerInvariant() switch
	{
		"i32" or "int" or "int32" => SnapshotFieldType.Int32,
		"u32" or "uint" or "uint32" => SnapshotFieldType.UInt32,
		"i64" or "long" or "int64" => SnapshotFieldType.Int64,
		"u64" or "ulong" or "uint64" => SnapshotFieldType.UInt64,
		"f32" or "float" or "single" => SnapshotFieldType.Single,
		"f64" or "double" => SnapshotFieldType.Double,
		"byte" or "u8" => SnapshotFieldType.Byte,
		"bool" or "boolean" => SnapshotFieldType.Boolean,
		_ => throw new ArgumentException($"Unsupported schema field type '{raw}'."),
	};
}

static void PrintUsage()
{
	Console.WriteLine("Usage:");
	Console.WriteLine("  EcsEngine.SnapshotInspector <snapshot.bin> [--format text|json] [--output <file>] [--schema <Type=field:type,...>]");
	Console.WriteLine();
	Console.WriteLine("Examples:");
	Console.WriteLine("  EcsEngine.SnapshotInspector snapshot.bin --format text");
	Console.WriteLine("  EcsEngine.SnapshotInspector snapshot.bin --format json --output snapshot.json \\");
	Console.WriteLine("    --schema EcsEngine.Replay.Tests.Position=X:float,Y:float --schema EcsEngine.Replay.Tests.Tag=Id:int");
}
