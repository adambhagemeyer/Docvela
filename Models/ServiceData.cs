namespace Docvela.Models;

public class ServiceData
{
    public string ServiceName { get; set; } = string.Empty;
    public List<ServiceMethodData> Methods { get; set; } = new();
    public List<DataModel> DataModelsUsed { get; set; } = new();
}