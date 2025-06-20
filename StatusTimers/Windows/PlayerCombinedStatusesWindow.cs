using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class PlayerCombinedStatusesWindow()
    : StatusTimerWindow<StatusKey>(new PlayerCombinedStatusesSource(), NodeKind.Combined);
