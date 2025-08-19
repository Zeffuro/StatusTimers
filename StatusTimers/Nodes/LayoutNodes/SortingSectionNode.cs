using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class SortingSectionNode : VerticalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly SectionHeaderNode _sectionHeaderNode;

    public SortingSectionNode(Func<StatusTimerOverlayConfig> getConfig, NodeKind kind) {
        _getConfig = getConfig;

        _sectionHeaderNode = new SectionHeaderNode("Sorting Settings");
        AddNode(_sectionHeaderNode);

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
        AddNode(new SortRowNode(
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
            FitPadding = 4
        });

        // Secondary Sort
        AddNode(new SortRowNode(
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
            FitPadding = 4
        });

        // Tertiary Sort
        AddNode(new SortRowNode(
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
            FitPadding = 4
        });

        RecalculateLayout();
    }
}
