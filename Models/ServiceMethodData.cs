namespace Docvela.Models;

public class ServiceMethodData
{
    public string MethodName { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterData> Parameters { get; set; } = new();
    public List<string> InternalCalls { get; set; } = new();
    public List<string> ReturnStatements { get; set; } = new();
    public string Summary { get; set; } = "";
}