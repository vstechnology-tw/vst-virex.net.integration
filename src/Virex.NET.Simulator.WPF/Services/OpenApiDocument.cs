using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

internal static class OpenApiDocument
{
    private static readonly string[] RequiredProductInfoFields = ["waferID", "lotID", "slot", "foupID", "chamberID"];

    public static object Create(string baseUrl) => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Virex.NET Simulator API",
            version = "2.0.2",
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
            [RestRoutes.ApiSystemDeinitialize] = new Dictionary<string, object> { ["post"] = CommandOperation("Deinitialize simulator", "Moves the simulator from Ready to Uninitialized.") },
            [RestRoutes.ApiSystemStart] = new Dictionary<string, object> { ["post"] = CommandOperation("Start run", "Starts a simulated run.", Ref("SystemStartRequest")) },
            [RestRoutes.ApiSystemStop] = new Dictionary<string, object> { ["post"] = CommandOperation("Stop run", "Stops the current simulated run.", Ref("SystemStopRequest")) },
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
                ["SystemStatus"] = ObjectSchema(new Dictionary<string, object> { ["state"] = StringSchema() }),
                ["ErrorInfo"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["hasError"] = BoolSchema(),
                    ["message"] = NullableStringSchema(),
                    ["state"] = StringSchema(),
                }),
                ["CommandResponse"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["accepted"] = BoolSchema(),
                    ["state"] = StringSchema(),
                    ["command"] = StringSchema(),
                    ["errorCode"] = NullableStringSchema(),
                    ["message"] = StringSchema(),
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
            },
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

    private static object NullableStringSchema() => new { type = "string", nullable = true };

    private static object BoolSchema() => new { type = "boolean" };

    private static object IntSchema() => new { type = "integer", format = "int32" };
}
