namespace Docvela.Models;

public class AnalyzerOutputData
{
    public string ControllerName { get; set; } = "";
    public List<EndpointData> Endpoints { get; set; } = new();
    public HashSet<DataModel> DataModelsUsed { get; set; } = new();
}
