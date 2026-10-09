namespace Virex.NET.Contracts;

/// <summary>Read-only query failure; does not imply a machine command or state transition.</summary>
public sealed class QueryError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class QueryErrorCodes
{
    public const string UnknownSource = "unknown_source";
    public const string NoCurrentResult = "no_current_result";
    public const string ResultNotCommitted = "result_not_committed";
    public const string SourceUnavailable = "source_unavailable";
    public const string SourceValueMissing = "source_value_missing";
    public const string ApiFailed = "api_failed";
    public const string NoCurrentRecipe = "no_current_recipe";
    public const string QueryNotReady = "query_not_ready";
    public const string ResultNotFound = "result_not_found";
    public const string ResultDeleted = "result_deleted";
    public const string QueryFailed = "query_failed";
    public const string InvalidQuery = "invalid_query";
}
