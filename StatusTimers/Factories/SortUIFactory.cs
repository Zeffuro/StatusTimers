using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Factories;

public static class SortUIFactory
{
    public static VerticalListNode<NodeBase> CreateSortPrioritySection(
        StatusTimerOverlayConfig? config,
        NodeKind kind)
    {
        var section = new VerticalListNode<NodeBase>
        {
            IsVisible = true,
            Width = 600,
            Height = 200,
            ItemVerticalSpacing = 2,
            FitContents = true
        };

        // Header
        section.AddNode(new TextNode
        {
            IsVisible = true,
            Width = 120,
            Height = TextStyles.Header.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.Header.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = "Sorting Priority"
        });

        Dictionary<SortCriterion, string> sortCriteriaMap = new() {
            { SortCriterion.None, "None" },
            { SortCriterion.StatusType, "Status Type" },
            { SortCriterion.TimeRemaining, "Time Remaining" },
            { SortCriterion.OwnStatusFirst, "Own Status First" },
            { SortCriterion.PartyPriority, "Party Priority" }
        };

        if (kind == NodeKind.MultiDoT) {
            sortCriteriaMap.Add(SortCriterion.EnemyLetter, "Enemy Letter");
        }

        // Primary Sort
        section.AddNode(CreateSortRow(
            "Primary:",
            () => config.PrimarySort,
            v => config.PrimarySort = v,
            () => config.PrimarySortOrder,
            v => config.PrimarySortOrder = v,
            sortCriteriaMap
        ));

        // Secondary Sort
        section.AddNode(CreateSortRow(
            "Secondary:",
            () => config.SecondarySort,
            v => config.SecondarySort = v,
            () => config.SecondarySortOrder,
            v => config.SecondarySortOrder = v,
            sortCriteriaMap
        ));

        // Tertiary Sort
        section.AddNode(CreateSortRow(
            "Tertiary:",
            () => config.TertiarySort,
            v => config.TertiarySort = v,
            () => config.TertiarySortOrder,
            v => config.TertiarySortOrder = v,
            sortCriteriaMap
        ));

        return section;
    }

    public static HorizontalFlexNode<NodeBase> CreateSortRow(
        string labelText,
        Func<SortCriterion> criterionGetter,
        Action<SortCriterion> criterionSetter,
        Func<SortOrder> orderGetter,
        Action<SortOrder> orderSetter,
        Dictionary<SortCriterion, string> criteriaMap)
    {
        var flexNode = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            X = ConfigurationUIFactory.OptionOffset,
            Width = 600,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        flexNode.AddNode(new TextNode
        {
            IsVisible = true,
            Width = 60,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = labelText
        });

        flexNode.AddNode(new TextDropDownNode
        {
            IsVisible = true,
            Width = 200,
            Height = 24,
            MaxListOptions = 5,
            Options = new List<string>(criteriaMap.Values),
            SelectedOption = criteriaMap.TryGetValue(criterionGetter(), out var selectedString) ? selectedString : criteriaMap.Values.First(),
            OnOptionSelected = selectedDisplayName =>
            {
                var selected = FindKeyByValue(criteriaMap, selectedDisplayName);
                if (selected != null) {
                    criterionSetter(selected.Value);
                }
            }
        });

        var orderMap = new Dictionary<SortOrder, string>
        {
            { SortOrder.Ascending, "Ascending" },
            { SortOrder.Descending, "Descending" }
        };

        flexNode.AddNode(new TextDropDownNode
        {
            IsVisible = true,
            Width = 180,
            Height = 24,
            MaxListOptions = 2,
            Options = new List<string>(orderMap.Values),
            SelectedOption = orderMap.TryGetValue(orderGetter(), out var sel) ? sel : orderMap.Values.First(),
            OnOptionSelected = selectedDisplayName =>
            {
                var selected = FindKeyByValue(orderMap, selectedDisplayName);
                if (selected != null) {
                    orderSetter(selected.Value);
                }
            }
        });

        return flexNode;
    }

    private static TKey? FindKeyByValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TValue value)
        where TKey : struct {
        foreach (var kvp in dict.Where(kvp => EqualityComparer<TValue>.Default.Equals(kvp.Value, value)))
        {
            return kvp.Key;
        }

        return null;
    }
}
