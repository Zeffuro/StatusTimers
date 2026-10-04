using System;
using System.Collections.Generic;

namespace StatusTimers.Config;

public sealed class ProfileStore
{
    public int Version { get; set; } = 1;
    public string ActiveProfile { get; set; } = "Default";
    public Dictionary<string, string> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<uint, string> JobProfiles { get; set; } = [];
    public bool AutoSwitchJobs { get; set; }
    public Dictionary<uint, string> HudLayoutProfiles { get; set; } = [];
    public bool AutoSwitchHudLayouts { get; set; }
}
