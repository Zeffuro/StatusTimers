using System.Collections.Generic;

namespace StatusTimers.Enums;

public static class TimerFormats {
    public static readonly Dictionary<string, string> Formats = new() {
        { "Auto", "{auto}" },
        { "Seconds (0.0s)", "{S.0}s" },
        { "Minutes (0:00)", "{m}:{ss}" },
        { "Minutes (0m 0s)", "{m}m {ss}s" },
        { "Hours:Minutes:Seconds (0:00:00)", "{H}:{mm}:{ss}" },
        { "Hours:Minutes:Seconds (0h:0m:0s)", "{H}h {mm}m {ss}s" },
    };
}

