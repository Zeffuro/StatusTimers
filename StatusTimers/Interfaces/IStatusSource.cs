using StatusTimers.Config;
using StatusTimers.Models;
using System.Collections.Generic;

namespace StatusTimers.Interfaces;

public interface IStatusSource<TKey> {
    IReadOnlyList<StatusInfo> Fetch(StatusTimerOverlayConfig? config);
    TKey KeyOf(StatusInfo info);
}
