using StatusTimers.Helpers;
using StatusTimers.Windows;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public class EnemyMultiDoTSource(EnemyMultiDoTOverlay overlay) : IStatusSource<StatusKey> {
    public IReadOnlyList<StatusInfo> Fetch(IOverlayConfiguration config) {
        return StatusManager.GetHostileStatuses(overlay);
    }

    public StatusKey KeyOf(StatusInfo info) {
        return info.Key;
    }
}
