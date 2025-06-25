using StatusTimers.Interfaces;
using StatusTimers.Models;
using StatusTimers.Services;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public class EnemyMultiDoTSource : IStatusSource<StatusKey> {
    public IReadOnlyList<StatusInfo> Fetch(IOverlayConfiguration config) {
        return StatusManager.GetHostileStatuses(config);
    }

    public StatusKey KeyOf(StatusInfo info) {
        return info.Key;
    }
}
