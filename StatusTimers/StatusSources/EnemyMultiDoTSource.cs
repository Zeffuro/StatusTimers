using System.Collections.Generic;
using StatusTimers.Helpers;

namespace StatusTimers.StatusSources;

public class EnemyMultiDoTSource : IStatusSource<StatusKey>
{
    public IReadOnlyList<StatusInfo> Fetch() => StatusManager.GetHostileStatuses();
    public StatusKey KeyOf(StatusInfo info) => info.Key;             // already inside
}