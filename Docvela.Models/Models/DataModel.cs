namespace Docvela.Models;

public class DataModel
{
    public string Name { get; set; } = string.Empty;
    public List<ParameterData> Properties { get; set; } = new();

    public override bool Equals(object? obj)
    {
        return obj is DataModel other && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
