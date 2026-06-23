using System.Collections.Generic;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

internal static class OpenApiDocument
{
    private static readonly string[] RequiredWaferInfoFields = ["lotId", "waferId", "slot", "foupId", "chamberId"];

    public static object Create(string baseUrl) => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Virex.NET Simulator API",
            version = "1.0.0",
        },
        servers = new[] { new { url = baseUrl.TrimEnd('/') } },
        paths = new Dictionary<string, object>
        {
            [RestRoutes.Health] = new Dictionary<string, object>
            {
                ["get"] = Operation(
                    "Health check",
                    "Service is reachable.",
                    Ref("HealthResponse")),
            },
            [RestRoutes.ApiStatus] = new Dictionary<string, object>
            {
                ["get"] = Operation(
                    "Get current simulator status",
                    "Current simulator initialization state, process state, and recipe.",
                    Ref("StatusDto")),
            },
            [RestRoutes.ApiError] = new Dictionary<string, object>
            {
                ["get"] = Operation(
                    "Get current simulator error",
                    "Current simulator error state.",
                    Ref("ErrorStatusDto")),
            },
            [RestRoutes.ApiWaferInfo] = new Dictionary<string, object>
            {
                ["get"] = Operation(
                    "Get current wafer information",
                    "Current wafer information used by the simulator.",
                    Ref("WaferInfo")),
                ["post"] = WaferInfoUpdateOperation(),
            },
            [RestRoutes.ApiControlInitialize] = new Dictionary<string, object>
            {
                ["post"] = ControlOperation(
                    "Initialize simulator",
                    "Moves the simulator into the initialized ready state."),
            },
            [RestRoutes.ApiControlTerminate] = new Dictionary<string, object>
            {
                ["post"] = ControlOperation(
                    "Terminate simulator",
                    "Returns the simulator to the terminated ready state."),
            },
            [RestRoutes.ApiControlStart] = new Dictionary<string, object>
            {
                ["post"] = ControlOperation(
                    "Start inspection cycle",
                    "Starts a fake inspection cycle. Returns 409 not_initialized if initialize has not been called."),
            },
            [RestRoutes.ApiControlStop] = new Dictionary<string, object>
            {
                ["post"] = ControlOperation(
                    "Stop inspection cycle",
                    "Stops the current fake inspection cycle."),
            },
            [RestRoutes.ApiResults] = new Dictionary<string, object>
            {
                ["get"] = new
                {
                    summary = "Query inspection results",
                    description = "Only lotId, waferId, and recipeId query parameters are supported. Multiple parameters are combined with AND.",
                    parameters = new object[]
                    {
                        QueryParameter("lotId", "Lot ID filter."),
                        QueryParameter("waferId", "Wafer ID filter."),
                        QueryParameter("recipeId", "Recipe ID filter."),
                    },
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = JsonResponse("Matching saved results.", Ref("ResultListDto")),
                    },
                },
            },
        },
        components = new
        {
            schemas = new Dictionary<string, object>
            {
                ["HealthResponse"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["status"] = StringSchema(),
                }),
                ["WaferInfo"] = ObjectSchema(
                    new Dictionary<string, object>
                    {
                        ["lotId"] = StringSchema(),
                        ["waferId"] = StringSchema(),
                        ["recipeId"] = StringSchema(),
                        ["slot"] = StringSchema(),
                        ["foupId"] = StringSchema(),
                        ["chamberId"] = StringSchema(),
                    },
                    RequiredWaferInfoFields),
                ["StatusDto"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["initialized"] = BoolSchema(),
                    ["processState"] = StringSchema(),
                    ["recipe"] = StringSchema(),
                }),
                ["ErrorStatusDto"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["hasError"] = BoolSchema(),
                    ["message"] = NullableStringSchema(),
                    ["initialized"] = BoolSchema(),
                    ["processState"] = StringSchema(),
                    ["recipe"] = StringSchema(),
                }),
                ["ControlStatusDto"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["initialized"] = BoolSchema(),
                    ["processState"] = StringSchema(),
                    ["recipe"] = StringSchema(),
                    ["message"] = StringSchema(),
                }),
                ["ResultSummaryDto"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["resultId"] = StringSchema(),
                    ["timestamp"] = StringSchema("date-time"),
                    ["lotId"] = StringSchema(),
                    ["waferId"] = StringSchema(),
                    ["recipeId"] = StringSchema(),
                    ["slot"] = StringSchema(),
                    ["foupId"] = StringSchema(),
                    ["chamberId"] = StringSchema(),
                    ["overallResult"] = StringSchema(),
                    ["defectCount"] = IntSchema(),
                    ["imageRelativePath"] = StringSchema(),
                    ["resultRelativePath"] = StringSchema(),
                    ["imagePath"] = StringSchema(),
                    ["previewImagePath"] = StringSchema(),
                    ["resultPath"] = StringSchema(),
                }),
                ["ResultListDto"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["items"] = new
                    {
                        type = "array",
                        items = Ref("ResultSummaryDto"),
                    },
                    ["count"] = IntSchema(),
                }),
                ["ErrorResponse"] = ObjectSchema(new Dictionary<string, object>
                {
                    ["error"] = StringSchema(),
                    ["message"] = StringSchema(),
                }),
            },
        },
    };

    private static object Operation(string summary, string description, object schema) => new
    {
        summary,
        description,
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse(description, schema),
        },
    };

    private static object WaferInfoUpdateOperation() => new
    {
        summary = "Update current wafer information",
        requestBody = new
        {
            required = true,
            content = new Dictionary<string, object>
            {
                ["application/json"] = new
                {
                    schema = Ref("WaferInfo"),
                },
            },
        },
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Wafer information updated.", Ref("WaferInfo")),
            ["400"] = JsonResponse("Invalid wafer information payload.", Ref("ErrorResponse")),
        },
    };

    private static object ControlOperation(string summary, string description) => new
    {
        summary,
        description,
        responses = new Dictionary<string, object>
        {
            ["200"] = JsonResponse("Command accepted.", Ref("ControlStatusDto")),
            ["409"] = JsonResponse("The simulator cannot execute the command from the current state.", Ref("ControlStatusDto")),
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
        content = new Dictionary<string, object>
        {
            ["application/json"] = new
            {
                schema,
            },
        },
    };

    private static object ObjectSchema(Dictionary<string, object> properties, string[]? required = null) => new
    {
        type = "object",
        required,
        properties,
    };

    private static object Ref(string name) => new Dictionary<string, object>
    {
        ["$ref"] = "#/components/schemas/" + name,
    };

    private static object StringSchema(string? format = null) => new
    {
        type = "string",
        format,
    };

    private static object NullableStringSchema() => new
    {
        type = "string",
        nullable = true,
    };

    private static object BoolSchema() => new { type = "boolean" };

    private static object IntSchema() => new { type = "integer", format = "int32" };
}
