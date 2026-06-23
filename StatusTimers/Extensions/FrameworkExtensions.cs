using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace StatusTimers.Extensions;

public static class FrameworkExtensions {
    public static Task RunSafely(this IFramework framework, Action runAction)
        => framework.IsFrameworkUnloading ? Task.CompletedTask : framework.Run(runAction);
}
