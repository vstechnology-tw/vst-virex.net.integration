namespace Virex.NET.Contracts;

public static class CommandErrorCodes
{
    public const string InvalidState = "invalid_state";
    public const string InvalidRunMode = "invalid_run_mode";
    public const string RequiresDeinitialize = "requires_deinitialize";
    public const string InvalidPayload = "invalid_payload";
    public const string CommandFailed = "command_failed";
    public const string QueryFailed = "query_failed";
}
