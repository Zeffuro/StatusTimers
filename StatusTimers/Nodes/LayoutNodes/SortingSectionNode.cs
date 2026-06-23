using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class SortingSectionNode : ConfigVerticalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly SectionHeaderNode _sectionHeaderNode;

    public SortingSectionNode(Func<StatusTimerOverlayConfig> getConfig, NodeKind kind) {
        _getConfig = getConfig;

        _sectionHeaderNode = new SectionHeaderNode("Sorting Settings");

        Dictionary<SortCriterion, string> sortCriteriaMap = new() {
            { SortCriterion.None, "None" },
            { SortCriterion.StatusType, "Status Type" },
            { SortCriterion.TimeRemaining, "Time Remaining" },
            { SortCriterion.OwnStatusFirst, "Own Status First" },
            { SortCriterion.PartyPriority, "Party Priority" },
            { SortCriterion.PermaIcon, "Permanent Icons" }
        };

        if (kind == NodeKind.MultiDoT) {
            sortCriteriaMap.Add(SortCriterion.EnemyLetter, "Enemy Letter");
        }

        // Primary Sort
        var primarySortNode = new SortRowNode(
            "Primary:",
            () => getConfig().PrimarySort,
            v => getConfig().PrimarySort = v,
            () => getConfig().PrimarySortOrder,
            v => getConfig().PrimarySortOrder = v,
            sortCriteriaMap
        ) {
            IsVisible = true,
            X = 18,
            Width = 600,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            ItemSpacing = 4
        };

        // Secondary Sort
        var secondarySortNode = new SortRowNode(
            "Secondary:",
            () => getConfig().SecondarySort,
            v => getConfig().SecondarySort = v,
            () => getConfig().SecondarySortOrder,
            v => getConfig().SecondarySortOrder = v,
            sortCriteriaMap
        ){
            IsVisible = true,
            X = 18,
            Width = 600,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            ItemSpacing = 4
        };

        // Tertiary Sort
        var tertiarySortNode = new SortRowNode(
            "Tertiary:",
            () => getConfig().TertiarySort,
            v => getConfig().TertiarySort = v,
            () => getConfig().TertiarySortOrder,
            v => getConfig().TertiarySortOrder = v,
            sortCriteriaMap
        ){
            IsVisible = true,
            X = 18,
            Width = 600,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            ItemSpacing = 4
        };

        AddNode([_sectionHeaderNode, primarySortNode, secondarySortNode, tertiarySortNode]);
        RecalculateLayout();
    }
}
