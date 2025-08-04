using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using Docvela.Models;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("Docvela SVG Initializer - generate SVG from JSON data");

        var inputOption = new Option<FileInfo>(
            "--input",
            description: "Path to the JSON input file")
        {
            IsRequired = true
        };

        var outputOption = new Option<DirectoryInfo>(
            "--output",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Output directory for SVG files");

        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler((FileInfo inputFile, DirectoryInfo outputDir) =>
        {
            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Input file {inputFile.FullName} does not exist.");
                return;
            }

            var json = File.ReadAllText(inputFile.FullName);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var inputData = JsonSerializer.Deserialize<InputData>(json, options);
            if (inputData == null)
            {
                Console.Error.WriteLine("Failed to deserialize input data.");
                return;
            }

            var visualizer = new SvgGenerator();
            var svg = visualizer.GenerateSvg(inputData);
            string outputFileName = "docvela_flowchart.svg";
            File.WriteAllText(Path.Combine(outputDir.FullName, outputFileName), svg);
            Console.WriteLine($"SVG files generated in {outputDir.FullName}");

        }, inputOption, outputOption);

        return rootCommand.InvokeAsync(args).Result;
    }
}

// Define InputData class that matches your JSON structure
public class InputData
{
    public List<AnalyzerOutputData> Controllers { get; set; }
    public List<ServiceData> Services { get; set; }
    public List<DataModel> DataModels { get; set; }
}
