using StatusTimers.Helpers;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public class EnemyMultiDoTSource : IStatusSource<StatusKey> {
    public IReadOnlyList<StatusInfo> Fetch() {
        return StatusManager.GetHostileStatuses();
    }

    public StatusKey KeyOf(StatusInfo info) {
        return info.Key;
        // already inside
    }
}
