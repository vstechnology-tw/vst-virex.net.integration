namespace Virex.NET.Contracts;

public static class CommandErrorCodes
{
    public const string CapturePreparationFailed = "capture_preparation_failed";
    public const string InvalidOperationMode = "invalid_operation_mode";
    public const string InvalidOperationSource = "invalid_operation_source";
    public const string OperationNotAllowed = "operation_not_allowed";
    public const string InvalidState = "invalid_state";
    public const string InvalidRunMode = "invalid_run_mode";
    public const string InvalidInspectionMode = "invalid_inspection_mode";
    public const string RequiresDeinitialize = "requires_deinitialize";
    public const string InvalidPayload = "invalid_payload";
    public const string CommandFailed = "command_failed";
    public const string QueryFailed = "query_failed";
}
