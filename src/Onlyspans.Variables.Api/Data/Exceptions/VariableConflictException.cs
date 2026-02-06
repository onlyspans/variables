namespace Onlyspans.Variables.Api.Data.Exceptions;

public class VariableConflictException : Exception
{
    public string Key { get; }
    public List<string> Sources { get; }

    public VariableConflictException(string key, List<string> sources)
        : base($"Variable '{key}' has conflicting values from multiple variable sets: {string.Join(", ", sources)}")
    {
        Key = key;
        Sources = sources;
    }
}
