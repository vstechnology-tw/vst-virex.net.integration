namespace Virex.NET.Contracts;

/// <summary>Read-only query failure; does not imply a machine command or state transition.</summary>
public sealed class QueryError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class QueryErrorCodes
{
    public const string NoCurrentRecipe = "no_current_recipe";
    public const string QueryNotReady = "query_not_ready";
    public const string ResultNotFound = "result_not_found";
    public const string ResultDeleted = "result_deleted";
    public const string QueryFailed = "query_failed";
    public const string InvalidQuery = "invalid_query";
}
