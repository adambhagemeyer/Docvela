using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Docvela.Services;
using System.Threading.Tasks;
using System.IO;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Docvela: .NET API Documentation Generator");

        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to the .NET API project folder or csproj file")
        {
            IsRequired = true
        };

        var analyzeCommand = new Command("analyze", "Analyze a .NET API project and generate docs");
        analyzeCommand.AddOption(pathOption);

        analyzeCommand.SetHandler(async (string path) =>
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Error: Directory '{path}' does not exist.");
                return;
            }

            // Scan controllers
            var apiScanner = new ApiScanner(path);
            var controllers = apiScanner.ScanControllers();

            // Scan services - either through ApiScanner or instantiate ServiceAnalyzer directly
            var serviceAnalyzer = new ServiceAnalyzer(path);
            var services = serviceAnalyzer.AnalyzeServices();

            // Docs root folder
            string docsFolder = Path.Combine(path, "docs");

            if (Directory.Exists(docsFolder))
                Directory.Delete(docsFolder, recursive: true);

            var generator = new MarkdownReportGenerator();

            // Generate docs with desired folder structure
            generator.Generate(controllers, Path.Combine(docsFolder, "controllers"));
            generator.WriteServiceMarkdown(docsFolder, services);

            // Write global DataModels file with unique models from controllers and services combined
            var allDataModels = controllers.SelectMany(c => c.DataModelsUsed)
                                          .Concat(services.SelectMany(s => s.DataModelsUsed))
                                          .GroupBy(dm => dm.Name)
                                          .Select(g => g.First())
                                          .ToList();

            generator.WriteGlobalDataModels(Path.Combine(docsFolder, "datamodels"), allDataModels);

            Console.WriteLine($"Docs generated successfully in: {docsFolder}");

            await Task.CompletedTask;
        }, pathOption);

        rootCommand.AddCommand(analyzeCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
