using System.Collections.Generic;
using StatusTimers.Helpers;

namespace StatusTimers.StatusSources;

public class PlayerCombinedStatusesSource : IStatusSource<StatusKey>
{
    public IReadOnlyList<StatusInfo> Fetch() => StatusManager.GetPlayerStatuses();
    public StatusKey KeyOf(StatusInfo info) => info.Key;
}