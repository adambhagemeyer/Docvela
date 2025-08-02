using Docvela.Models;
using System.Text;

namespace Docvela.Services;

public class MarkdownReportGenerator
{
    public void Generate(List<AnalyzerOutputData> data, string docsFolder)
    {
        Directory.CreateDirectory(docsFolder);

        foreach (var controller in data)
        {
            var filePath = Path.Combine(docsFolder, $"{controller.ControllerName}_Analysis.md");
            using var writer = new StreamWriter(filePath);

            writer.WriteLine($"# {controller.ControllerName}\n");

            if (controller.DataModelsUsed.Any())
            {
                writer.WriteLine("## Data Models Used\n");
                foreach (var model in controller.DataModelsUsed)
                {
                    writer.WriteLine($"### {model.Name}");
                    foreach (var prop in model.Properties)
                        writer.WriteLine($"- `{prop.Type}` {prop.Name}");
                    writer.WriteLine();
                }
            }

            writer.WriteLine("## Endpoints\n");
            foreach (var endpoint in controller.Endpoints)
            {
                writer.WriteLine($"### {endpoint.HttpMethod} {endpoint.MethodSignature}");
                if (!string.IsNullOrWhiteSpace(endpoint.Route))
                    writer.WriteLine($"- **Route:** `{endpoint.Route}`");

                if (endpoint.Parameters.Any())
                {
                    writer.WriteLine("- **Parameters:**");
                    foreach (var param in endpoint.Parameters)
                        writer.WriteLine($"  - `{param.Type}` {param.Name}");
                }

                if (endpoint.ServiceCalls.Any())
                {
                    writer.WriteLine("- **Service Calls:**");
                    foreach (var call in endpoint.ServiceCalls)
                        writer.WriteLine($"  - `{call}`");
                }

                if (endpoint.ReturnStatements.Any())
                {
                    writer.WriteLine("- **Return Values:**");
                    foreach (var r in endpoint.ReturnStatements)
                        writer.WriteLine($"  - `{r.Statement}`");
                }

                writer.WriteLine();
            }
        }
    }

    public void WriteGlobalDataModels(string docsFolder, List<DataModel> dataModels)
    {
        Directory.CreateDirectory(docsFolder);

        var path = Path.Combine(docsFolder, "DataModels.md");
        using var writer = new StreamWriter(path);

        writer.WriteLine("# Data Models\n");

        foreach (var model in dataModels.OrderBy(m => m.Name))
        {
            writer.WriteLine($"## {model.Name}");
            foreach (var prop in model.Properties)
                writer.WriteLine($"- {prop.Type} {prop.Name}");
            writer.WriteLine();
        }
    }

    public void WriteServiceMarkdown(string docsFolder, List<ServiceData> services)
    {
        var servicesFolder = Path.Combine(docsFolder, "services");

        // Clear old markdown files in this folder before generating new ones
        if (Directory.Exists(servicesFolder))
        {
            foreach (var oldFile in Directory.GetFiles(servicesFolder, "*.md"))
                File.Delete(oldFile);
        }

        Directory.CreateDirectory(servicesFolder);

        foreach (var service in services)
        {
            var nonEmptyModels = service.DataModelsUsed
                .Where(dm => dm.Properties != null && dm.Properties.Count > 0)
                .ToList();

            var nonEmptyMethods = service.Methods
                .Where(m =>
                    !string.IsNullOrWhiteSpace(m.MethodName) ||
                    !string.IsNullOrWhiteSpace(m.ReturnType) ||
                    m.Parameters.Any() ||
                    m.InternalCalls.Any() ||
                    m.ReturnStatements.Any())
                .ToList();

            // If the service has no content, skip
            if (!nonEmptyModels.Any() && !nonEmptyMethods.Any())
                continue;

            var filePath = Path.Combine(servicesFolder, $"{service.ServiceName}_Service.md");
            using var writer = new StreamWriter(filePath);

            writer.WriteLine($"# Service: {service.ServiceName}\n");

            if (nonEmptyModels.Any())
            {
                writer.WriteLine("## Data Models Used\n");
                foreach (var model in nonEmptyModels)
                {
                    writer.WriteLine($"### {model.Name}");
                    foreach (var prop in model.Properties)
                        writer.WriteLine($"- `{prop.Type}` {prop.Name}");
                    writer.WriteLine();
                }
            }

            writer.WriteLine("## Methods\n");
            foreach (var method in nonEmptyMethods)
            {
                writer.WriteLine($"### {method.MethodName}()");
                writer.WriteLine($"- Returns: `{method.ReturnType}`");

                if (method.Parameters.Any())
                {
                    writer.WriteLine("- Parameters:");
                    foreach (var p in method.Parameters)
                        writer.WriteLine($"  - `{p.Type}` {p.Name}");
                }

                if (method.InternalCalls.Any())
                {
                    writer.WriteLine("- Internal Calls:");
                    foreach (var call in method.InternalCalls)
                        writer.WriteLine($"  - `{call}`");
                }

                if (method.ReturnStatements.Any())
                {
                    writer.WriteLine("- Return Statements:");
                    foreach (var ret in method.ReturnStatements)
                        writer.WriteLine($"  - `{ret}`");
                }

                writer.WriteLine();
            }
        }
    }
}
