namespace Docvela.Models;

public class EndpointData
{
    public string HttpMethod { get; set; } = "";
    public string Route { get; set; } = "";
    public string MethodSignature { get; set; } = "";
    public List<ParameterData> Parameters { get; set; } = new();
    public List<ReturnData> ReturnStatements { get; set; } = new();
    public List<string> ServiceCalls { get; set; } = new();

    // NEW: Models used directly by this endpoint
    public List<DataModel> DataModelsUsed { get; set; } = new();

    public string Summary { get; set; } = "";
}

