using System.Text.Json;
using Docvela.Models;

namespace Docvela.Services;

public class JsonReportGenerator
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public void WriteAllDataJson(string outputFolder, List<AnalyzerOutputData> controllers, List<ServiceData> services, List<DataModel> dataModels)
    {
        Directory.CreateDirectory(outputFolder);
        var filePath = Path.Combine(outputFolder, "api-data.json");

        var combined = new
        {
            Controllers = controllers,
            Services = services,
            DataModels = dataModels.OrderBy(dm => dm.Name)
        };

        var json = JsonSerializer.Serialize(combined, _options);
        File.WriteAllText(filePath, json);
    }

    public void Generate(List<AnalyzerOutputData> controllers, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        foreach (var controller in controllers)
        {
            var filePath = Path.Combine(outputFolder, $"{controller.ControllerName}_Analysis.json");
            var json = JsonSerializer.Serialize(controller, _options);
            File.WriteAllText(filePath, json);
        }
    }

    public void WriteServiceJson(string outputFolder, List<ServiceData> services)
    {
        var servicesFolder = Path.Combine(outputFolder, "services");
        Directory.CreateDirectory(servicesFolder);

        foreach (var service in services)
        {
            var filePath = Path.Combine(servicesFolder, $"{service.ServiceName}_Service.json");
            var json = JsonSerializer.Serialize(service, _options);
            File.WriteAllText(filePath, json);
        }
    }

    public void WriteGlobalDataModels(string outputFolder, List<DataModel> dataModels)
    {
        Directory.CreateDirectory(outputFolder);
        var filePath = Path.Combine(outputFolder, "DataModels.json");
        var json = JsonSerializer.Serialize(dataModels.OrderBy(dm => dm.Name), _options);
        File.WriteAllText(filePath, json);
    }
}
