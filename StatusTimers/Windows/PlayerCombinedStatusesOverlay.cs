using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class PlayerCombinedStatusesOverlay : StatusTimerOverlay<StatusKey> {
    public PlayerCombinedStatusesOverlay() : base(NodeKind.Combined) {
        Source = new PlayerCombinedStatusesSource(this);
    }
}
