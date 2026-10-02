using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

internal static class OpenApiDocument
{
    private static readonly string[] RequiredProductInfoFields = ["waferID", "lotID", "recipe", "slot", "foupID", "chamberID"];
    private static readonly string[] ParameterTypes = ["string", "boolean", "integer", "number"];

    public static object Create(string baseUrl) => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Virex.NET Simulator API",
            version = "2.2.3",
        },
        servers = new[] { new { url = baseUrl.TrimEnd('/') } },
        paths = new Dictionary<string, object>
        {
            [RestRoutes.Health] = new Dictionary<string, object> { ["get"] = Operation("Health check", "Service is reachable.", Ref("HealthResponse")) },
            [RestRoutes.ApiStatus] = new Dictionary<string, object> { ["get"] = Operation("Get current simulator status", "Current simulator state.", Ref("SystemStatus")) },
            [RestRoutes.ApiError] = new Dictionary<string, object> { ["get"] = Operation("Get current simulator error", "Current simulator error state.", Ref("ErrorInfo")) },
            [RestRoutes.ApiProductInfo] = new Dictionary<string, object>
            {
                ["get"] = Operation("Get current product information", "Current product information used by the simulator.", Ref("ProductInfo")),
                ["post"] = ProductInfoUpdateOperation(),
            },
            [RestRoutes.ApiSystemInitialize] = new Dictionary<string, object> { ["post"] = CommandOperation("Initialize simulator", "Moves the simulator from Uninitialized to Ready.") },
            [RestRoutes.ApiSystemDeinitialize] = new Dictionary<string, object> { ["post"] = CommandOperation("Deinitialize simulator", "Moves the simulator from Ready to Uninitialized and retries cleanup from public Deinitializing recovery state.") },
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
                    ["condition"] = NullableStringSchema(),
                    ["runMode"] = NullableStringSchema(),
                }),
                ["SystemStopRequest"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["reason"] = NullableStringSchema(),
                }),
                ["ResultSummary"] = ObjectSchema(new Dictionary<string, object>
                {
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
                    ["recipe"] = StringSchema(), ["revision"] = StringSchema(),
                }, ["recipe", "revision"]),
                ["RecipeList"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["items"] = new { type = "array", items = Ref("RecipeInfo") }, ["count"] = IntSchema(),
                }, ["items", "count"]),
                ["RecipeParameters"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["recipe"] = StringSchema(), ["revision"] = StringSchema(),
                    ["groups"] = new { type = "array", items = Ref("RecipeParameterGroup") },
                }, ["recipe", "revision", "groups"]),
                ["RecipeParameterGroup"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["key"] = StringSchema(), ["parameters"] = new { type = "array", items = Ref("RecipeParameter") },
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
                    ["resultId"] = StringSchema(), ["summary"] = Ref("ResultSummary"),
                    ["findings"] = new { type = "array", items = Ref("ResultFinding") },
                }, ["schemaVersion", "resultId", "summary", "findings"]),
                ["ResultFinding"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["findingId"] = StringSchema(), ["kind"] = StringSchema(), ["label"] = StringSchema(),
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
                    ["errorCode"] = StringSchema(), ["message"] = StringSchema(),
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
            ["404"] = JsonResponse("result_not_found: unknown, uncommitted or outside retained history.", Ref("QueryError")),
            ["405"] = JsonResponse("invalid_query: GET only; Allow: GET.", Ref("QueryError")),
            ["409"] = JsonResponse("no_current_recipe or query_not_ready: no loaded recipe or snapshot unavailable during transition.", Ref("QueryError")),
            ["410"] = JsonResponse("result_deleted: indexed committed artifact deleted.", Ref("QueryError")),
            ["503"] = JsonResponse("query_failed: snapshot read failed, corrupt or inconsistent artifact.", Ref("QueryError")),
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
