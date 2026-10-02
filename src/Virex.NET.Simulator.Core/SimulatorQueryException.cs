namespace Virex.NET.Simulator.Core;

/// <summary>A public read-only query failure, without command rejection or machine state changes.</summary>
public sealed class SimulatorQueryException : Exception
{
    public SimulatorQueryException(int statusCode, string errorCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }
}
