using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using System.Collections.Generic;

namespace StatusTimers.Interfaces;

public interface IStatusSource<TKey> {
    StatusCategory? StatusCategoryFilter { get; }
    IReadOnlyList<StatusInfo> Fetch(StatusTimerOverlayConfig? config);
    TKey KeyOf(StatusInfo info);
}
