namespace Virex.NET.Contracts;

/// <summary>Public recipe identity. Revision identifies the loaded configuration snapshot.</summary>
public sealed class RecipeInfo
{
    public string Recipe { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
}

public sealed class RecipeList
{
    public RecipeInfo[] Items { get; set; } = [];
    public int Count { get; set; }
}
