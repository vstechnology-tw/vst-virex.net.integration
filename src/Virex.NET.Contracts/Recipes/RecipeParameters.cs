using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Parameters of one loaded recipe snapshot; compare Recipe and Revision across queries.</summary>
public sealed class RecipeParameters
{
    public string Recipe { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public RecipeParameterGroup[] Groups { get; set; } = [];
}

public sealed class RecipeParameterGroup
{
    public string Key { get; set; } = string.Empty;
    public RecipeParameter[] Parameters { get; set; } = [];
}

public sealed class RecipeParameter
{
    public string Key { get; set; } = string.Empty;
    /// <summary>One of string, boolean, integer, number. Value uses the corresponding JSON scalar.</summary>
    public string Type { get; set; } = string.Empty;
    public JsonElement Value { get; set; }
}
