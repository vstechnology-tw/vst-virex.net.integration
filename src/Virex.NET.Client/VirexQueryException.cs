using Virex.NET.Contracts;

namespace Virex.NET.Client;

/// <summary>A typed data-source query failure. StatusCode is zero for a transport/invalid-response failure.</summary>
public sealed class VirexQueryException : Exception
{
    public VirexQueryException(int statusCode, QueryError error, string responseBody = "", Exception? innerException = null)
        : base(error.Message, innerException)
    {
        StatusCode = statusCode; Error = error; ResponseBody = responseBody;
    }
    public int StatusCode { get; }
    public QueryError Error { get; }
    public string ErrorCode => Error.ErrorCode;
    public string ResponseBody { get; }
}
