using Mediator;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed class InitializeSystemCommand : ICommand<CommandResponse>
{
}

public sealed class DeinitializeSystemCommand : ICommand<CommandResponse>
{
}

public sealed class SetProductInfoCommand : ICommand<CommandResponse>
{
    public SetProductInfoCommand(ProductInfo productInfo)
    {
        ProductInfo = productInfo;
    }

    public ProductInfo ProductInfo { get; }
}

public sealed class StartSystemCommand : ICommand<CommandResponse>
{
    public StartSystemCommand(SystemStartRequest request)
    {
        Request = request;
    }

    public SystemStartRequest Request { get; }
}

public sealed class StopSystemCommand : ICommand<CommandResponse>
{
    public StopSystemCommand(SystemStopRequest request)
    {
        Request = request;
    }

    public SystemStopRequest Request { get; }
}

public sealed class InitializationCompletedEvent : INotification
{
}

public sealed class ProductInfoUpdateCompletedEvent : INotification
{
}

public sealed class RunCompletedEvent : INotification
{
}

public sealed class DeinitializationCompletedEvent : INotification
{
}

public sealed class StatusChangedEvent : INotification
{
    public StatusChangedEvent(SystemStatus status)
    {
        Status = status;
    }

    public SystemStatus Status { get; }
}

public sealed class ProductInfoChangedEvent : INotification
{
    public ProductInfoChangedEvent(ProductInfo productInfo)
    {
        ProductInfo = productInfo;
    }

    public ProductInfo ProductInfo { get; }
}

public sealed class ResultCreatedEvent : INotification
{
    public ResultCreatedEvent(ResultSummary result)
    {
        Result = result;
    }

    public ResultSummary Result { get; }
}

public sealed class CommandRejectedEvent : INotification
{
    public CommandRejectedEvent(CommandResponse response)
    {
        Response = response;
    }

    public CommandResponse Response { get; }
}
