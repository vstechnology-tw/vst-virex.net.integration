using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

internal static class OpenApiDocument
{
    private static readonly string[] RequiredProductInfoFields = ["waferID", "lotID", "recipe", "slot", "foupID", "chamberID"];
    private static readonly string[] ParameterTypes = ["string", "boolean", "integer", "number"];
    private static readonly string[] AlarmCategoryValues = [AlarmCategories.Fault, AlarmCategories.Defect, AlarmCategories.CriticalDefect];
    private static readonly string[] RequiredAlarmCatalogFields = ["appId", "instanceId", "timestamp", "sources"];
    private static readonly string[] RequiredAlarmSourceFields = ["name", "source", "category"];
    private static readonly string[] RequiredAlarmNotificationFields = ["name", "source", "category", "message", "state", "timestamp", "appId", "instanceId"];

    public static object Create(string baseUrl) => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Virex.NET Simulator API",
            version = typeof(CommandResponse).Assembly.GetName().Version?.ToString(3) ?? "unknown",
        },
        servers = new[] { new { url = baseUrl.TrimEnd('/') } },
        paths = new Dictionary<string, object>
        {
            [RestRoutes.ApiAlarms] = new Dictionary<string, object> { ["get"] = ReadQueryOperation("Discover actual Alarm producers and their exact TCP event names", "AlarmCatalog") },
            [RestRoutes.ApiSystemReset] = new Dictionary<string, object> { ["post"] = CommandOperation("Reset after stop and required cleanup", "External source; Local rejects without side effects. Concurrent requests join cleanup; successful reset returns Uninitialized, never Ready. Cleanup failure remains Faulted. Caller disconnect/cancellation does not abandon admitted cleanup; explicit Initialize is required.") },
            [RestRoutes.ApiDataSources] = new Dictionary<string, object> { ["get"] = DataSourceQueryOperation("Discover source names, kinds, JSON types and current-work availability", "DataSourceCatalog") },
            [RestRoutes.ApiDataSourceValue] = new Dictionary<string, object> { ["get"] = DataSourceQueryOperation("Read one exact live source or explicitly selected committed result source", "DataSourceValue", valueQuery: true) },
            [RestRoutes.ApiCurrentResult] = new Dictionary<string, object> { ["get"] = DataSourceQueryOperation("Read the current committed result identity; no implicit result value", "ResultReference") },
            [RestRoutes.ApiEvents] = new Dictionary<string, object>
            {
                ["get"] = Operation("List supported event names", "Exact case-sensitive producer names; App identity distinguishes instances and restarts.", Ref("EventCatalog")),
            },
            [RestRoutes.ApiOperationMode] = new Dictionary<string, object>
            {
                ["get"] = Operation("Get operation mode", "Independent of SystemStatus; managementEnabled is a host opt-in.", Ref("OperationModeInfo")),
                ["post"] = CommandOperation("Apply operation mode", "External Local to Remote is allowed; success means applied. Source is host-assigned.", Ref("SetOperationModeRequest")),
            },
            [RestRoutes.Health] = new Dictionary<string, object> { ["get"] = Operation("Health check", "Service is reachable.", Ref("HealthResponse")) },
            [RestRoutes.ApiStatus] = new Dictionary<string, object> { ["get"] = Operation("Get current simulator status", "Current simulator state.", Ref("SystemStatus")) },
            [RestRoutes.ApiError] = new Dictionary<string, object> { ["get"] = Operation("Get current simulator error", "Current simulator error state.", Ref("ErrorInfo")) },
            [RestRoutes.ApiProductInfo] = new Dictionary<string, object>
            {
                ["get"] = Operation("Get current product information", "Current product information used by the simulator.", Ref("ProductInfo")),
                ["post"] = ProductInfoUpdateOperation(),
            },
            [RestRoutes.ApiSystemInitialize] = new Dictionary<string, object> { ["post"] = CommandOperation("Initialize simulator", "Moves the simulator from Uninitialized to Ready.") },
            [RestRoutes.ApiSystemDeinitialize] = new Dictionary<string, object> { ["post"] = CommandOperation("Deinitialize simulator", "Moves Ready or Faulted to Uninitialized after cleanup; failures remain Faulted. Overlapping Reset shares its cleanup owner.") },
            [RestRoutes.ApiSystemStart] = new Dictionary<string, object> { ["post"] = CommandOperation("Start run", "Starts a simulated run.", Ref("SystemStartRequest")) },
            [RestRoutes.ApiSystemStop] = new Dictionary<string, object> { ["post"] = CommandOperation("Stop run", "Stops the current simulated run.", Ref("SystemStopRequest")) },
            [RestRoutes.ApiRecipes] = new Dictionary<string, object> { ["get"] = ReadQueryOperation("List available recipes", "RecipeList") },
            [RestRoutes.ApiCurrentRecipe] = new Dictionary<string, object> { ["get"] = ReadQueryOperation("Read the actually loaded recipe", "RecipeInfo") },
            [RestRoutes.ApiCurrentRecipeParameters] = new Dictionary<string, object> { ["get"] = ReadQueryOperation("Read one loaded parameter snapshot; compare recipe and revision across calls", "RecipeParameters") },
            [RestRoutes.ApiResultDetail] = new Dictionary<string, object> { ["get"] = ReadQueryOperation("Read the exact committed ResultId; never select by Lot/Wafer or latest result", "ResultDetail", resultDetail: true) },
            [RestRoutes.ApiResults] = new Dictionary<string, object>
            {
                ["get"] = new
                {
                    summary = "Query inspection results",
                    description = "Only waferID, lotID, and recipe query parameters are supported.",
                    parameters = new object[]
                    {
                        QueryParameter("waferID", "Product ID filter."),
                        QueryParameter("lotID", "Lot ID filter."),
                        QueryParameter("recipe", "Recipe ID filter."),
                    },
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = JsonResponse("Matching saved results.", Ref("ResultList")),
                    },
                },
            },
        },
        components = new
        {
            schemas = new Dictionary<string, object>
            {
                ["AlarmCatalog"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = StringSchema(), ["instanceId"] = StringSchema(), ["timestamp"] = StringSchema("date-time"),
                    ["sources"] = new { type = "array", items = Ref("AlarmSourceDescriptor") },
                }, RequiredAlarmCatalogFields),
                ["AlarmSourceDescriptor"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["name"] = new { type = "string", description = "Exact case-sensitive event/producer name. No SDK whitelist or customer ID." },
                    ["source"] = StringSchema(), ["category"] = new { type = "string", @enum = AlarmCategoryValues },
                }, RequiredAlarmSourceFields),
                ["AlarmNotification"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["name"] = StringSchema(), ["source"] = StringSchema(), ["category"] = new { type = "string", @enum = AlarmCategoryValues },
                    ["message"] = StringSchema(), ["state"] = StringSchema(), ["timestamp"] = StringSchema("date-time"),
                    ["appId"] = StringSchema(), ["instanceId"] = StringSchema(), ["requestId"] = StringSchema(),
                    ["jobId"] = StringSchema(), ["captureId"] = StringSchema(), ["resultId"] = StringSchema(),
                }, RequiredAlarmNotificationFields),
                ["DataSourceCatalog"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = StringSchema(), ["instanceId"] = StringSchema(), ["timestamp"] = StringSchema("date-time"),
                    ["sources"] = new { type = "array", items = Ref("DataSourceDescriptor") },
                }, ["appId", "instanceId", "timestamp", "sources"]),
                ["DataSourceDescriptor"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["name"] = StringSchema(),
                    ["kind"] = new { type = "string", @enum = new[] { DataSourceKinds.Live, DataSourceKinds.Result } },
                    ["type"] = new { type = "string", @enum = new[] { "string", "boolean", "integer", "number", "object", "array" } },
                    ["availability"] = new { type = "string", @enum = new[] { DataSourceAvailability.Available, DataSourceAvailability.NoCurrentResult, DataSourceAvailability.Unavailable, DataSourceAvailability.MissingValue } },
                }, ["name", "kind", "type", "availability"]),
                ["DataSourceValue"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = StringSchema(), ["instanceId"] = StringSchema(), ["timestamp"] = StringSchema("date-time"),
                    ["name"] = StringSchema(), ["kind"] = new { type = "string", @enum = new[] { DataSourceKinds.Live, DataSourceKinds.Result } },
                    ["type"] = new { type = "string", @enum = new[] { "string", "boolean", "integer", "number", "object", "array" } },
                    ["state"] = StringSchema(), ["requestId"] = StringSchema(), ["jobId"] = StringSchema(),
                    ["captureId"] = StringSchema(), ["resultId"] = StringSchema(),
                    ["value"] = new { description = "Owned JSON snapshot matching the declared type. Null is a failure. Result sources require jobId, captureId and the exact requested resultId." },
                }, ["appId", "instanceId", "timestamp", "name", "kind", "type", "state", "value"]),
                ["ResultReference"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = StringSchema(), ["instanceId"] = StringSchema(), ["timestamp"] = StringSchema("date-time"),
                    ["requestId"] = StringSchema(), ["jobId"] = StringSchema(), ["captureId"] = StringSchema(), ["resultId"] = StringSchema(),
                }, ["appId", "instanceId", "timestamp", "jobId", "captureId", "resultId"]),
                ["EventCatalog"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = StringSchema(),
                    ["instanceId"] = StringSchema(),
                    ["names"] = new { type = "array", uniqueItems = true, items = StringSchema() },
                }, ["appId", "instanceId", "names"]),
                ["EventEnvelope"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["type"] = new { type = "string", @enum = new[] { "event" } },
                    ["name"] = StringSchema(),
                    ["eventId"] = StringSchema(),
                    ["timestamp"] = StringSchema("date-time"),
                    ["appId"] = StringSchema(),
                    ["instanceId"] = StringSchema(),
                    ["connectionGeneration"] = StringSchema(),
                    ["requestId"] = StringSchema(),
                    ["jobId"] = StringSchema(),
                    ["captureId"] = StringSchema(),
                    ["resultId"] = StringSchema(),
                    ["payload"] = new { type = "object", additionalProperties = true },
                }, ["type", "name", "eventId", "timestamp", "appId", "instanceId", "connectionGeneration", "payload"]),
                ["OperationModeInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["mode"] = new { type = "string", @enum = new[] { OperationModes.Local, OperationModes.Remote } },
                    ["managementEnabled"] = BoolSchema(),
                }, ["mode", "managementEnabled"]),
                ["CaptureReadyInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["jobId"] = StringSchema(),
                    ["captureId"] = StringSchema(),
                    ["timestamp"] = StringSchema("date-time"),
                    ["sourceIds"] = new { type = "array", minItems = 1, uniqueItems = true, items = StringSchema() },
                }, ["jobId", "captureId", "timestamp", "sourceIds"]),
                ["CaptureCompletedInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["jobId"] = StringSchema(),
                    ["captureId"] = StringSchema(),
                    ["timestamp"] = StringSchema("date-time"),
                    ["sourceIds"] = new { type = "array", minItems = 1, uniqueItems = true, items = StringSchema() },
                    ["imageCount"] = new { type = "integer", minimum = 1 },
                }, ["jobId", "captureId", "timestamp", "sourceIds", "imageCount"]),
                ["SetOperationModeRequest"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["mode"] = new { type = "string", @enum = new[] { OperationModes.Local, OperationModes.Remote } },
                }, ["mode"]),
                ["HealthResponse"] = ObjectSchema(new Dictionary<string, object> { ["status"] = StringSchema() }),
                ["ProductInfo"] = ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["waferID"] = StringSchema(),
                        ["lotID"] = StringSchema(),
                        ["recipe"] = StringSchema(),
                        ["slot"] = StringSchema(),
                        ["foupID"] = StringSchema(),
                        ["chamberID"] = StringSchema(),
                    },
                    RequiredProductInfoFields),
                ["SystemStatus"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["state"] = StringSchema(),
                    ["recoveryAction"] = NullableStringSchema(),
                    ["recoveryStartedAt"] = NullableStringSchema("date-time"),
                    ["errorCode"] = NullableStringSchema(),
                    ["recoverySource"] = NullableStringSchema(),
                    ["recoveryPhase"] = NullableStringSchema(),
                    ["recoveryDetails"] = NullableStringSchema(),
                }),
                ["ErrorInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["hasError"] = BoolSchema(),
                    ["message"] = NullableStringSchema(),
                    ["state"] = StringSchema(),
                    ["recoveryAction"] = NullableStringSchema(),
                    ["errorCode"] = NullableStringSchema(),
                    ["recoveryStartedAt"] = NullableStringSchema("date-time"),
                    ["recoverySource"] = NullableStringSchema(),
                    ["recoveryPhase"] = NullableStringSchema(),
                    ["recoveryDetails"] = NullableStringSchema(),
                }),
                ["CommandResponse"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["appId"] = NullableStringSchema(),
                    ["instanceId"] = NullableStringSchema(),
                    ["jobId"] = NullableStringSchema(),
                    ["captureId"] = NullableStringSchema(),
                    ["operationMode"] = Ref("OperationModeInfo"),
                    ["accepted"] = BoolSchema(),
                    ["state"] = StringSchema(),
                    ["command"] = StringSchema(),
                    ["requestId"] = NullableStringSchema(),
                    ["errorCode"] = NullableStringSchema(),
                    ["message"] = StringSchema(),
                    ["recoveryAction"] = NullableStringSchema(),
                    ["recoveryStartedAt"] = NullableStringSchema("date-time"),
                    ["recoverySource"] = NullableStringSchema(),
                    ["recoveryPhase"] = NullableStringSchema(),
                    ["recoveryDetails"] = NullableStringSchema(),
                }),
                ["SystemStartRequest"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["requestId"] = new { type = "string", minLength = 1, description = "Created before sending by the SDK; correlates racing acceptance and work events." },
                    ["condition"] = NullableStringSchema(),
                    ["runMode"] = NullableStringSchema(),
                    ["inspectionMode"] = new { type = "string", nullable = true, @enum = new[] { InspectionModes.CaptureOnly, InspectionModes.CaptureAndInspect }, description = "Optional per-start selection. Omitted/null preserves recipe behavior; independent of runMode." },
                }),
                ["SystemStopRequest"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["reason"] = NullableStringSchema(),
                }),
                ["ResultSummary"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["jobId"] = NullableStringSchema(),
                    ["resultId"] = StringSchema(),
                    ["captureId"] = StringSchema(),
                    ["timestamp"] = StringSchema("date-time"),
                    ["waferID"] = StringSchema(),
                    ["lotID"] = StringSchema(),
                    ["recipe"] = StringSchema(),
                    ["slot"] = StringSchema(),
                    ["foupID"] = StringSchema(),
                    ["chamberID"] = StringSchema(),
                    ["condition"] = StringSchema(),
                    ["overallResult"] = StringSchema(),
                    ["defectCount"] = IntSchema(),
                    ["imageRelativePath"] = StringSchema(),
                    ["resultRelativePath"] = StringSchema(),
                    ["imagePath"] = StringSchema(),
                    ["previewImagePath"] = StringSchema(),
                    ["resultPath"] = StringSchema(),
                }),
                ["ResultList"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["items"] = new { type = "array", items = Ref("ResultSummary") },
                    ["count"] = IntSchema(),
                }),
                ["RecipeInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["recipe"] = StringSchema(),
                    ["revision"] = StringSchema(),
                }, ["recipe", "revision"]),
                ["RecipeList"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["items"] = new { type = "array", items = Ref("RecipeInfo") },
                    ["count"] = IntSchema(),
                }, ["items", "count"]),
                ["RecipeParameters"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["recipe"] = StringSchema(),
                    ["revision"] = StringSchema(),
                    ["groups"] = new { type = "array", items = Ref("RecipeParameterGroup") },
                }, ["recipe", "revision", "groups"]),
                ["RecipeParameterGroup"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["key"] = StringSchema(),
                    ["parameters"] = new { type = "array", items = Ref("RecipeParameter") },
                }, ["key", "parameters"]),
                ["RecipeParameter"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["key"] = StringSchema(),
                    ["type"] = new { type = "string", @enum = ParameterTypes },
                    ["value"] = new { oneOf = new object[] { StringSchema(), BoolSchema(), new { type = "number" } }, description = "JSON scalar matching type; integer must be an integral Int64 value." },
                }, ["key", "type", "value"]),
                ["ResultDetail"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["schemaVersion"] = new { type = "integer", @enum = new[] { ResultDetail.CurrentSchemaVersion } },
                    ["resultId"] = StringSchema(),
                    ["summary"] = Ref("ResultSummary"),
                    ["findings"] = new { type = "array", items = Ref("ResultFinding") },
                }, ["schemaVersion", "resultId", "summary", "findings"]),
                ["ResultFinding"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["findingId"] = StringSchema(),
                    ["kind"] = StringSchema(),
                    ["label"] = StringSchema(),
                    ["score"] = new { type = "number", nullable = true },
                    ["productPolygon"] = new { type = "array", items = Ref("ProductPoint") },
                    ["diagnosticImageIds"] = new { type = "array", items = StringSchema() },
                }, ["findingId", "kind", "label", "productPolygon", "diagnosticImageIds"]),
                ["ProductPoint"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["xmm"] = new { type = "number", description = "Product-frame X in millimeters." },
                    ["ymm"] = new { type = "number", description = "Product-frame Y in millimeters." },
                }, ["xmm", "ymm"]),
                ["QueryError"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["errorCode"] = StringSchema(),
                    ["message"] = StringSchema(),
                }, ["errorCode", "message"]),
            },
        },
    };

    private static object ReadQueryOperation(string summary, string schema, bool resultDetail = false) => new
    {
        summary,
        description = "Read-only. No query parameters. Failures do not change machine state or emit command/error events.",
        parameters = resultDetail ? new object[] { new { name = "resultId", @in = "path", required = true, schema = StringSchema(), description = "Exact, case-sensitive ResultId, URL-encoded as one path segment." } } : Array.Empty<object>(),
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Snapshot; an empty findings array is valid.", Ref(schema)),
            ["400"] = JsonResponse("invalid_query: query parameters or empty identifier.", Ref("QueryError")),
            ["404"] = JsonResponse("result_not_found: unknown or outside retained history.", Ref("QueryError")),
            ["405"] = JsonResponse("invalid_query: GET only; Allow: GET.", Ref("QueryError")),
            ["409"] = JsonResponse("no_current_recipe, query_not_ready or result_not_committed: unavailable snapshot or known capture without a committed result.", Ref("QueryError")),
            ["410"] = JsonResponse("result_deleted: indexed committed artifact deleted.", Ref("QueryError")),
            ["503"] = JsonResponse("query_failed: snapshot read failed, corrupt or inconsistent artifact.", Ref("QueryError")),
        },
    };

    private static object DataSourceQueryOperation(string summary, string schema, bool valueQuery = false) => new
    {
        summary,
        description = "Read-only. Names and ResultIds are exact case-sensitive strings. Result values require explicit resultId; no latest-result fallback. Other query parameters and writes are rejected.",
        parameters = valueQuery ? new object[]
        {
            new { name = "name", @in = "query", required = true, schema = StringSchema() },
            new { name = "resultId", @in = "query", required = false, schema = StringSchema(), description = "Required for a result source; forbidden for a live source." },
        } : Array.Empty<object>(),
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Typed snapshot; empty findings and real zero values are valid.", Ref(schema)),
            ["400"] = JsonResponse("invalid_query", Ref("QueryError")),
            ["404"] = JsonResponse("unknown_source or result_not_found", Ref("QueryError")),
            ["405"] = JsonResponse("invalid_query; Allow: GET", Ref("QueryError")),
            ["409"] = JsonResponse("no_current_result, result_not_committed, source_unavailable or source_value_missing", Ref("QueryError")),
            ["410"] = JsonResponse("result_deleted", Ref("QueryError")),
            ["503"] = JsonResponse("query_failed", Ref("QueryError")),
        },
    };

    private static object Operation(string summary, string description, object schema) => new
    {
        summary,
        description,
        responses = new Dictionary<string, object> { ["200"] = JsonResponse(description, schema) },
    };

    private static object ProductInfoUpdateOperation() => new
    {
        summary = "Update current product information",
        requestBody = new
        {
            required = true,
            content = new Dictionary<string, object> { ["application/json"] = new { schema = Ref("ProductInfo") } },
        },
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Product information updated.", Ref("CommandResponse")),
            ["400"] = JsonResponse("Invalid JSON payload.", Ref("CommandResponse")),
            ["503"] = JsonResponse("Operation failed.", Ref("CommandResponse")),
            ["409"] = JsonResponse("Product information cannot be updated from the current state.", Ref("CommandResponse")),
        },
    };

    private static object CommandOperation(string summary, string description, object? requestSchema = null) => new
    {
        summary,
        description,
        requestBody = requestSchema is null ? null : new
        {
            required = false,
            content = new Dictionary<string, object> { ["application/json"] = new { schema = requestSchema } },
        },
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Command accepted.", Ref("CommandResponse")),
            ["400"] = JsonResponse("Invalid JSON payload or run mode.", Ref("CommandResponse")),
            ["503"] = JsonResponse("Operation failed.", Ref("CommandResponse")),
            ["409"] = JsonResponse("The simulator cannot execute the command from the current state.", Ref("CommandResponse")),
        },
    };

    private static object QueryParameter(string name, string description) => new
    {
        name,
        @in = "query",
        required = false,
        description,
        schema = StringSchema(),
    };

    private static object JsonResponse(string description, object schema) => new
    {
        description,
        content = new Dictionary<string, object> { ["application/json"] = new { schema } },
    };

    private static object ObjectSchema(Dictionary<string, object> properties, string[]? required = null) => new
    {
        type = "object",
        required,
        properties,
    };

    private static object Ref(string name) => new Dictionary<string, object> { ["$ref"] = "#/components/schemas/" + name };

    private static object StringSchema(string? format = null) => new { type = "string", format };

    private static object NullableStringSchema(string? format = null) => new { type = "string", format, nullable = true };

    private static object BoolSchema() => new { type = "boolean" };

    private static object IntSchema() => new { type = "integer", format = "int32" };
}
