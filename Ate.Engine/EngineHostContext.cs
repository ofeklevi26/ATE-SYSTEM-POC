using Ate.Engine.Commands;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;

namespace Ate.Engine;

public static class EngineHostContext
{
    public static ILogger Logger { get; set; } = null!;

    public static DriverRegistry DriverRegistry { get; set; } = null!;

    public static CommandInvoker CommandInvoker { get; set; } = null!;
}
