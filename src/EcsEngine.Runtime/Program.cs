using EcsEngine.Runtime;
using EcsEngine.Transport;

string configPath = args.Length > 0 ? args[0] : "runtime.config.json";
RuntimeConfig config = RuntimeConfigLoader.LoadFromFile(configPath);

RuntimeHostBuilder builder = new RuntimeHostBuilder()
	.UseConfiguration(config);

if (config.StartupMode == StartupMode.Server)
	builder.UseTransport(new InMemoryTransport());

RuntimeHost host = builder.Build();
Console.WriteLine($"EcsEngine.Runtime started in {host.StartupMode} mode.");
