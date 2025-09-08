using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;

namespace StatusTimers.Logic;

public static class StatusSorter {
    public static Comparison<StatusTimerNode<TKey>> GetNodeComparison<TKey>(
        SortCriterion primarySort,
        SortOrder primaryOrder,
        SortCriterion secondarySort,
        SortOrder secondaryOrder,
        SortCriterion tertiarySort,
        SortOrder tertiaryOrder)
        where TKey : notnull
    {
        return (a, b) =>
        {
            int result = CompareByCriterion(a.StatusInfo, b.StatusInfo, primarySort, primaryOrder);
            if (result != 0) {
                return result;
            }

            result = CompareByCriterion(a.StatusInfo, b.StatusInfo, secondarySort, secondaryOrder);
            if (result != 0) {
                return result;
            }

            result = CompareByCriterion(a.StatusInfo, b.StatusInfo, tertiarySort, tertiaryOrder);
            return result;
        };
    }

    private static int CompareByCriterion(StatusInfo a, StatusInfo b, SortCriterion criterion, SortOrder order)
    {
        var selector = GetKeySelector(criterion);
        var va = selector(a);
        var vb = selector(b);

        int cmp = Comparer<object>.Default.Compare(va, vb);
        return order == SortOrder.Ascending ? cmp : -cmp;
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
