using StatusTimers.Models;
using System.Collections.Generic;

namespace StatusTimers.Interfaces;

public interface IStatusSource<TKey> {
    IReadOnlyList<StatusInfo> Fetch(IOverlayConfiguration config);
    TKey KeyOf(StatusInfo info);
}
