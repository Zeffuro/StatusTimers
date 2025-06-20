using StatusTimers.Helpers;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public class PlayerCombinedStatusesSource : IStatusSource<StatusKey> {
    public IReadOnlyList<StatusInfo> Fetch() {
        return StatusManager.GetPlayerStatuses();
    }

    public StatusKey KeyOf(StatusInfo info) {
        return info.Key;
    }
}
