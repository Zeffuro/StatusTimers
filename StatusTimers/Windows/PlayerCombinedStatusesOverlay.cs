using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class PlayerCombinedStatusesOverlay()
    : StatusTimerOverlay<StatusKey>(new PlayerCombinedStatusesSource(), NodeKind.Combined);
