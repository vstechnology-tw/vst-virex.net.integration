using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public enum SimulatorState
{
    Uninitialized,
    Initializing,
    Ready,
    UpdatingProductInfo,
    Running,
    Deinitializing,
}

public enum SimulatorTrigger
{
    Initialize,
    InitializationCompleted,
    SetProductInfo,
    ProductInfoUpdateCompleted,
    Start,
    Stop,
    RunCompleted,
    Deinitialize,
    DeinitializationCompleted,
}

public static class SimulatorStateNames
{
    public static string ToDto(SimulatorState state) =>
        state switch
        {
            SimulatorState.Uninitialized => SystemStates.Uninitialized,
            SimulatorState.Initializing => SystemStates.Initializing,
            SimulatorState.Ready => SystemStates.Ready,
            SimulatorState.UpdatingProductInfo => SystemStates.UpdatingProductInfo,
            SimulatorState.Running => SystemStates.Running,
            SimulatorState.Deinitializing => SystemStates.Deinitializing,
            _ => SystemStates.Uninitialized,
        };
}
