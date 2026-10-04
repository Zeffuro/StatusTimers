using StatusTimers.Enums;
using System.Collections.Generic;

namespace StatusTimers.Config;

public sealed class OverlaySetup
{
    public Dictionary<NodeKind, StatusTimerOverlayConfig> Overlays { get; set; } = [];
}
