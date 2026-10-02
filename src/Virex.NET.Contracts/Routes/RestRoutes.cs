namespace Virex.NET.Contracts;

public static class RestRoutes
{
    public const string ApiOperationMode = "/api/operation-mode";
    public const string Health = "/health";
    public const string OpenApiJson = "/openapi/v1.json";
    public const string Scalar = "/scalar";
    public const string ApiStatus = "/api/status";
    public const string ApiError = "/api/error";
    public const string ApiProductInfo = "/api/product-info";
    public const string ApiSystemInitialize = "/api/system/initialize";
    public const string ApiSystemDeinitialize = "/api/system/deinitialize";
    public const string ApiSystemStart = "/api/system/start";
    public const string ApiSystemStop = "/api/system/stop";
    public const string ApiResults = "/api/results";
    public const string ApiRecipes = "/api/recipes";
    public const string ApiCurrentRecipe = "/api/recipes/current";
    public const string ApiCurrentRecipeParameters = "/api/recipes/current/parameters";
    public const string ApiResultDetail = "/api/results/{resultId}";

    public static string ResultDetail(string resultId)
    {
        if (string.IsNullOrWhiteSpace(resultId) || resultId == "." || resultId == "..")
            throw new ArgumentException("A nonempty result identifier is required.", nameof(resultId));
        return ApiResults + "/" + Uri.EscapeDataString(resultId);
    }
}
