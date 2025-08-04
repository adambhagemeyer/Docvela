using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Docvela.Services;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;  // For JSON serialization

class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var rootCommand = new RootCommand("Docvela: .NET API Documentation Generator");

        rootCommand.Description = @"
  _____   ____   _______      ________ _               
 |  __ \ / __ \ / ____\ \    / /  ____| |        /\    
 | |  | | |  | | |     \ \  / /| |__  | |       /  \   
 | |  | | |  | | |      \ \/ / |  __| | |      / /\ \  
 | |__| | |__| | |____   \  /  | |____| |____ / ____ \ 
 |_____/ \____/ \_____|   \/   |______|______/_/    \_\

             A documentation generator
";

        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to the .NET API project folder or csproj file")
        {
            IsRequired = true
        };

        var jsonFlag = new Option<bool>(
            name: "--json",
            description: "Generate output in JSON format instead of SVG visualization",
            getDefaultValue: () => false);

        var analyzeCommand = new Command("analyze", "Analyze a .NET API project and generate docs");
        analyzeCommand.AddOption(pathOption);
        analyzeCommand.AddOption(jsonFlag);

        analyzeCommand.SetHandler(async (string path, bool jsonOutput) =>
        {
            if (!Directory.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Directory '{path}' does not exist.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("📦 Starting analysis...");
            ShowLoadingBar("🔍 Scanning project", 25);

            var apiScanner = new ApiScanner(path);
            var controllers = apiScanner.ScanControllers();

            var serviceAnalyzer = new ServiceAnalyzer(path);
            var services = serviceAnalyzer.AnalyzeServices();

            string docsFolder = Path.Combine(path, "docs");

            if (Directory.Exists(docsFolder))
                Directory.Delete(docsFolder, recursive: true);

            var allDataModels = controllers.SelectMany(c => c.DataModelsUsed)
                                          .Concat(services.SelectMany(s => s.DataModelsUsed))
                                          .GroupBy(dm => dm.Name)
                                          .Select(g => g.First())
                                          .ToList();

            if (jsonOutput)
            {

                var jsonGenerator = new JsonReportGenerator();
                jsonGenerator.WriteAllDataJson(docsFolder, controllers, services, allDataModels);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ JSON docs generated successfully in: {docsFolder}");
                Console.ResetColor();
            }
            else
            {
                var markdownGenerator = new MarkdownReportGenerator();
                markdownGenerator.Generate(controllers, Path.Combine(docsFolder, "controllers"));
                markdownGenerator.WriteServiceMarkdown(docsFolder, services);
                markdownGenerator.WriteGlobalDataModels(Path.Combine(docsFolder, "datamodels"), allDataModels);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✅ Markdown docs generated successfully in: {docsFolder}");
                Console.ResetColor();
            }

            await Task.CompletedTask;
        }, pathOption, jsonFlag);

        rootCommand.AddCommand(analyzeCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static void ShowLoadingBar(string message, int steps = 20, int delayMs = 50)
    {
        Console.Write(message + " ");
        for (int i = 0; i < steps; i++)
        {
            Console.Write("■");
            Thread.Sleep(delayMs);
        }
        Console.WriteLine();
    }
}
