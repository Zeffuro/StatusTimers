using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class VisualSectionNode : VerticalListNode
{
    public VisualSectionNode(
        StatusTimerOverlayNode<StatusKey> overlay,
        Func<StatusTimerOverlayConfig> getConfig)
    {
        AddNode(new SectionHeaderNode("Visual Settings"));

        // Locked and Preview Mode
        AddNode(new TwoOptionsRowNode(
            new CheckboxOptionNode {
                String = "Locked",
                IsChecked = overlay.IsLocked,
                OnClick = isChecked => { overlay.IsLocked = isChecked; },
                Tooltip = "When locked, the overlay cannot be moved or resized."
            },
            new CheckboxOptionNode {
                String = "Preview Mode",
                IsChecked = overlay.IsPreviewEnabled,
                OnClick = isChecked => { overlay.IsPreviewEnabled = isChecked; },
                Tooltip = "When enabled, will show random statuses to test with."
            },
            20
        ));

        AddDummy(16);

        // Node Height and Width
        AddNode(new TwoOptionsRowNode(
            new LabeledNumericOptionNode("Container Width",
                () => getConfig().RowWidth,
                value => { getConfig().RowWidth = value; },
                false
            ),
            new LabeledNumericOptionNode("Container Height",
                () => getConfig().RowHeight,
                value => { getConfig().RowHeight = value; },
                false
            ),
            20
        ));

        // Padding
        AddNode(new TwoOptionsRowNode(
            new LabeledNumericOptionNode("Horizontal Padding",
                () => getConfig().StatusHorizontalPadding,
                value => { getConfig().StatusHorizontalPadding = value; },
                false
            ),
            new LabeledNumericOptionNode("Vertical Padding",
                () => getConfig().StatusVerticalPadding,
                value => { getConfig().StatusVerticalPadding = value; },
                false
            ),
            20
        ));

        AddDummy(16);

        // Scale Label
        AddNode(new TwoOptionsRowNode(
            new TextNode
            {
                IsVisible = true,
                X = 18,
                Width = 120,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                AlignmentType = AlignmentType.Left,
                String = "Scale"
            },
            new ResNode {
                IsVisible = true,
                Width = 300,
                Height = 30
            },
            16
        ));

        // Scale slider
        AddNode(new TwoOptionsRowNode(
            new SliderNode {
                Range = new Range(5, 200),
                Step = 5,
                Value = getConfig().ScaleInt,
                OnValueChanged = value => { getConfig().ScaleInt = value; },
                X = 18,
                Width = 280,
                Height = 24,
                IsVisible = true
            },
            new ResNode {
                IsVisible = true,
                Width = 300,
                Height = 20
            },
            30
        ));

        // Statuses per row/column and Max statuses displayed
        TextNode statusPerLineNode = new()
        {
            X = 18,
            IsVisible = true,
            Width = 300,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            String = $"Statuses per {(getConfig().FillRowsFirst ? "row" : "column")}"
        };

        AddNode(new TwoOptionsRowNode(
            statusPerLineNode,
            new TextNode
            {
                X = 18,
                IsVisible = true,
                Width = 300,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                AlignmentType = AlignmentType.Left,
                String = "Max statuses displayed"
            },
            TextStyles.OptionLabel.Height
        ));

        // Sliders for ItemsPerLine and MaxStatuses
        AddNode(new TwoOptionsRowNode(
            new SliderNode {
                Range = new Range(1, 30),
                Step = 1,
                Value = getConfig().ItemsPerLine,
                OnValueChanged = value => { getConfig().ItemsPerLine = value; },
                X = 18,
                Width = 280,
                Height = 24,
                IsVisible = true
            },
            new SliderNode {
                Range = new Range(1, 30),
                Step = 1,
                Value = getConfig().MaxStatuses,
                OnValueChanged = value => { getConfig().MaxStatuses = value; },
                X = 18,
                Width = 280,
                Height = 24,
                IsVisible = true
            },
            30
        ));

        // Fill columns first + Grow direction dropdown
        AddNode(new TwoOptionsRowNode(
            new CheckboxOptionNode {
                String = "Fill columns first",
                IsChecked = !getConfig().FillRowsFirst,
                OnClick = isChecked => {
                    getConfig().FillRowsFirst = !isChecked;
                    statusPerLineNode.String = $"Statuses per {(getConfig().FillRowsFirst ? "row" : "column")}";
                }
            },
            new LabeledDropdownOptionNode<GrowDirection>(
                "Grow direction",
                () => getConfig().GrowDirection,
                value => { getConfig().GrowDirection = value;},
                new Dictionary<GrowDirection, string>
                {
                    { GrowDirection.DownRight, "Down and Right" },
                    { GrowDirection.DownLeft, "Down and Left" },
                    { GrowDirection.UpRight,   "Up and Right" },
                    { GrowDirection.UpLeft,    "Up and Left" }
                }
            ),
            20
        ));

        // Animations
        AddNode(new CheckboxOptionNode {
            String = "Animations enabled",
            IsChecked = getConfig().AnimationsEnabled,
            OnClick = value => {
                getConfig().AnimationsEnabled = value;
            }
        });

        RecalculateLayout();
    }
}
