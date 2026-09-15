using HalalChain.Platform.Api.CodeGen;

if (args.Length > 0 && args[0] == "--generate")
{
    var configPath = args.FirstOrDefault(a => a.StartsWith("--config="))?.Split('=')[1];
    var projectDir = args.FirstOrDefault(a => a.StartsWith("--project-dir="))?.Split('=')[1];

    if (configPath is null || projectDir is null)
    {
        Console.Error.WriteLine("Usage: generator --generate --config <path> --project-dir <path>");
        return 1;
    }

    var cfg = GeneratorConfigLoader.Load(Path.Combine(projectDir, configPath));
    var generator = new Generator(cfg, projectDir, Console.WriteLine);
    var result = generator.Run();

    Console.WriteLine($"Generated {result.PageCount} pages.");
    return 0;
}

Console.WriteLine("Generator ready.");
return 0;