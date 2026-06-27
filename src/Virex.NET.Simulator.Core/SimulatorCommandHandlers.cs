using Mediator;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed class InitializeSystemCommandHandler : ICommandHandler<InitializeSystemCommand, CommandResponse>
{
    private readonly SimulatorSession _session;

    public InitializeSystemCommandHandler(SimulatorSession session) => _session = session;

    public ValueTask<CommandResponse> Handle(InitializeSystemCommand command, CancellationToken cancellationToken) =>
        new ValueTask<CommandResponse>(_session.HandleInitializeAsync(cancellationToken));
}

public sealed class DeinitializeSystemCommandHandler : ICommandHandler<DeinitializeSystemCommand, CommandResponse>
{
    private readonly SimulatorSession _session;

    public DeinitializeSystemCommandHandler(SimulatorSession session) => _session = session;

    public ValueTask<CommandResponse> Handle(DeinitializeSystemCommand command, CancellationToken cancellationToken) =>
        new ValueTask<CommandResponse>(_session.HandleDeinitializeAsync(cancellationToken));
}

public sealed class SetProductInfoCommandHandler : ICommandHandler<SetProductInfoCommand, CommandResponse>
{
    private readonly SimulatorSession _session;

    public SetProductInfoCommandHandler(SimulatorSession session) => _session = session;

    public ValueTask<CommandResponse> Handle(SetProductInfoCommand command, CancellationToken cancellationToken) =>
        new ValueTask<CommandResponse>(_session.HandleSetProductInfoAsync(command.ProductInfo, cancellationToken));
}

public sealed class StartSystemCommandHandler : ICommandHandler<StartSystemCommand, CommandResponse>
{
    private readonly SimulatorSession _session;

    public StartSystemCommandHandler(SimulatorSession session) => _session = session;

    public ValueTask<CommandResponse> Handle(StartSystemCommand command, CancellationToken cancellationToken) =>
        new ValueTask<CommandResponse>(_session.HandleStartAsync(command.Request, cancellationToken));
}

public sealed class StopSystemCommandHandler : ICommandHandler<StopSystemCommand, CommandResponse>
{
    private readonly SimulatorSession _session;

    public StopSystemCommandHandler(SimulatorSession session) => _session = session;

    public ValueTask<CommandResponse> Handle(StopSystemCommand command, CancellationToken cancellationToken) =>
        new ValueTask<CommandResponse>(_session.HandleStopAsync(command.Request, cancellationToken));
}
