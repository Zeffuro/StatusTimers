using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Models;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.StatusSources;

public sealed class StatusCategoryFilteredSource<TKey>(
    IStatusSource<TKey> innerSource,
    StatusCategory statusCategory) : IStatusSource<TKey> {
    public StatusCategory? StatusCategoryFilter => statusCategory;

    public IReadOnlyList<StatusInfo> Fetch(StatusTimerOverlayConfig? config)
        => innerSource.Fetch(config)
            .Where(status => status.StatusType == statusCategory)
            .ToList();

    public TKey KeyOf(StatusInfo info)
        => innerSource.KeyOf(info);
}
