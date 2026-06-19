using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

internal static class OpenApiDocument
{
    public static object Create(string baseUrl) => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Virex.NET Simulator API",
            version = "1.0.0",
        },
        servers = new[] { new { url = baseUrl.TrimEnd('/') } },
        paths = new
        {
            status = Path("GET", RestRoutes.ApiStatus),
            error = Path("GET", RestRoutes.ApiError),
            waferInfo = Path("GET/POST", RestRoutes.ApiWaferInfo),
            initialize = Path("POST", RestRoutes.ApiControlInitialize),
            terminate = Path("POST", RestRoutes.ApiControlTerminate),
            start = Path("POST", RestRoutes.ApiControlStart),
            stop = Path("POST", RestRoutes.ApiControlStop),
            results = Path("GET", RestRoutes.ApiResults),
        },
    };

    private static object Path(string method, string route) => new { method, route };
}
