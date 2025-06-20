using StatusTimers.Helpers;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public interface IStatusSource<TKey> {
    /// Returns the *current* statuses for this window.
    IReadOnlyList<StatusInfo> Fetch();

    /// Extract a unique key for dictionary use.
    TKey KeyOf(StatusInfo info);
}
