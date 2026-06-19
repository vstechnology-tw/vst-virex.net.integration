using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class ControlResponse
{
    public int StatusCode { get; set; } = 200;

    public ControlStatusDto Body { get; set; } = new ControlStatusDto();
}
