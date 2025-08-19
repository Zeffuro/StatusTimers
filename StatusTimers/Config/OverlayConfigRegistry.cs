using StatusTimers.Enums;
using System.Collections.Generic;

namespace StatusTimers.Config;

public static class OverlayConfigRegistry
{
    private static readonly Dictionary<NodeKind, StatusTimerOverlayConfig> Configs = new();

    public static void Register(NodeKind kind, StatusTimerOverlayConfig config)
        => Configs[kind] = config;

    public static StatusTimerOverlayConfig? Get(NodeKind kind)
        => Configs.GetValueOrDefault(kind);
}
