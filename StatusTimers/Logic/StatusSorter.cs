using StatusTimers.Enums;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Logic;

public static class StatusSorter {
    public static IOrderedEnumerable<StatusInfo> ApplyAllSorts(
        IEnumerable<StatusInfo> statuses,
        SortCriterion primarySort,
        SortOrder primaryOrder,
        SortCriterion secondarySort,
        SortOrder secondaryOrder,
        SortCriterion tertiarySort,
        SortOrder tertiaryOrder) {
        IOrderedEnumerable<StatusInfo>? orderedList = null;

        IEnumerable<StatusInfo> statusInfos = statuses as StatusInfo[] ?? statuses.ToArray();
        if (primarySort != SortCriterion.None) {
            orderedList = ApplySingleSort(statusInfos, primarySort, primaryOrder);
        }
        else {
            orderedList = statusInfos.OrderBy(status => 0);
        }

        if (secondarySort != SortCriterion.None) {
            orderedList = ApplyThenBySort(orderedList, secondarySort, secondaryOrder);
        }

        if (tertiarySort != SortCriterion.None) {
            orderedList = ApplyThenBySort(orderedList, tertiarySort, tertiaryOrder);
        }

        return orderedList; // Fallback if somehow still null
    }

    private static IOrderedEnumerable<StatusInfo> ApplySingleSort(IEnumerable<StatusInfo> list, SortCriterion criterion,
        SortOrder order) {
        // Get the key selector function for the specified criterion.
        Func<StatusInfo, object?> keySelector = GetKeySelector(criterion);

        return order == SortOrder.Ascending
            ? list.OrderBy(keySelector) // Directly pass the keySelector Func
            : list.OrderByDescending(keySelector); // Directly pass the keySelector Func
    }

    private static IOrderedEnumerable<StatusInfo> ApplyThenBySort(IOrderedEnumerable<StatusInfo> orderedList,
        SortCriterion criterion, SortOrder order) {
        // Get the key selector function for the specified criterion.
        Func<StatusInfo, object?> keySelector = GetKeySelector(criterion);

        return order == SortOrder.Ascending
            ? orderedList.ThenBy(keySelector) // Directly pass the keySelector Func
            : orderedList.ThenByDescending(keySelector); // Directly pass the keySelector Func
    }

    private static Func<StatusInfo, object?> GetKeySelector(SortCriterion criterion) {
        return status => criterion switch {
            SortCriterion.StatusType => status.StatusType,
            SortCriterion.TimeRemaining => status.RemainingSeconds,
            SortCriterion.OwnStatusFirst => status.SelfInflicted,
            SortCriterion.PartyPriority => status.PartyPriority,
            SortCriterion.EnemyLetter => status.EnemyLetter,
            SortCriterion.PermaIcon => status.IsPermanent,
            _ => 0
        };
    }
}
