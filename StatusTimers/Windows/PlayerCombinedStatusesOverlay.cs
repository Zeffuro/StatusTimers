using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class PlayerCombinedStatusesOverlay()
    : StatusTimerOverlay<StatusKey>(NodeKind.Combined, new PlayerCombinedStatusesSource());
