using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Models;
using StatusTimers.Services;
using System.Collections.Generic;

namespace StatusTimers.StatusSources;

public class PlayerCombinedStatusesSource : IStatusSource<StatusKey> {
    public StatusCategory? StatusCategoryFilter => null;

    public IReadOnlyList<StatusInfo> Fetch(StatusTimerOverlayConfig? config) {
        return StatusManager.GetPlayerStatuses(config);
    }

    public StatusKey KeyOf(StatusInfo info) {
        return info.Key;
    }
}
