using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Factories;
using StatusTimers.Helpers;
using StatusTimers.Windows;

namespace StatusTimers.Factories;

public static class VisualSettingsUIFactory
{
    public static VerticalListNode Create(
        StatusTimerOverlay<StatusKey> overlay,
        Func<StatusTimerOverlayConfig> getConfig,
        Action? onChanged = null,
        float optionOffset = 18,
        float checkBoxHeight = 16)
    {
        var node = new VerticalListNode
        {
            IsVisible = true,
            ItemSpacing = 3,
            FitContents = true
        };

        // Header
        node.AddNode(new TextNode
        {
            IsVisible = true,
            Width = 120,
            Height = TextStyles.Header.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.Header.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = "Visual Settings"
        });

        // Locked and Preview Mode
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateCheckboxOption("Locked",
                () => overlay.IsLocked,
                isChecked => { overlay.IsLocked = isChecked; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateCheckboxOption("Preview Mode",
                () => overlay.IsPreviewEnabled,
                isChecked => { overlay.IsPreviewEnabled = isChecked; onChanged?.Invoke(); }
            ),
            checkBoxHeight
        ));

        node.AddNode();
        node.AddDummy(checkBoxHeight);

        // Node Height and Width
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateLabeledNumericOption("Container Width",
                () => getConfig().RowWidth,
                value => { getConfig().RowWidth = value; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateLabeledNumericOption("Container Height",
                () => getConfig().RowHeight,
                value => { getConfig().RowHeight = value; onChanged?.Invoke(); }
            ),
            20
        ));

        // Padding
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateLabeledNumericOption("Horizontal Padding",
                () => getConfig().StatusHorizontalPadding,
                value => { getConfig().StatusHorizontalPadding = value; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateLabeledNumericOption("Vertical Padding",
                () => getConfig().StatusVerticalPadding,
                value => { getConfig().StatusVerticalPadding = value; onChanged?.Invoke(); }
            ),
            20
        ));

        node.AddDummy(checkBoxHeight);

        // Scale Label
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            new TextNode
            {
                IsVisible = true,
                X = optionOffset,
                Width = 120,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                AlignmentType = AlignmentType.Left,
                Text = "Scale"
            },
            new ResNode {
                IsVisible = true,
                Width = 300,
                Height = 30
            },
            16
        ));

        // Scale slider
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateSliderOption(
                5,
                200,
                5,
                () => getConfig().ScaleInt,
                value => { getConfig().ScaleInt = value; onChanged?.Invoke(); }
            ),
            new ResNode {
                IsVisible = true,
                Width = 300,
                Height = 30
            },
            30
        ));

        // Statuses per row/column and Max statuses displayed
        TextNode statusPerLineNode = new()
        {
            X = optionOffset,
            IsVisible = true,
            Width = 300,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = $"Statuses per {(getConfig().FillRowsFirst ? "row" : "column")}"
        };

        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            statusPerLineNode,
            new TextNode
            {
                X = optionOffset,
                IsVisible = true,
                Width = 300,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                AlignmentType = AlignmentType.Left,
                Text = "Max statuses displayed"
            },
            TextStyles.OptionLabel.Height
        ));

        // Sliders for ItemsPerLine and MaxStatuses
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateSliderOption(
                1,
                30,
                1,
                () => getConfig().ItemsPerLine,
                value => { getConfig().ItemsPerLine = value; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateSliderOption(
                1,
                30,
                1,
                () => getConfig().MaxStatuses,
                value => { getConfig().MaxStatuses = value; onChanged?.Invoke(); }
            ),
            30
        ));

        // Fill columns first + Grow direction dropdown
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateCheckboxOption("Fill columns first",
                () => !getConfig().FillRowsFirst,
                isChecked =>
                {
                    getConfig().FillRowsFirst = !isChecked;
                    statusPerLineNode.Text = $"Statuses per {(getConfig().FillRowsFirst ? "row" : "column")}";
                    onChanged?.Invoke();
                }),
            ConfigurationUIFactory.CreateLabeledDropdown(
                "Grow direction",
                () => getConfig().GrowDirection,
                value => { getConfig().GrowDirection = value; onChanged?.Invoke(); },
                new System.Collections.Generic.Dictionary<GrowDirection, string>
                {
                    { GrowDirection.DownRight, "Down and Right" },
                    { GrowDirection.DownLeft, "Down and Left" },
                    { GrowDirection.UpRight,   "Up and Right" },
                    { GrowDirection.UpLeft,    "Up and Left" }
                }
            ),
            checkBoxHeight
        ));

        // Animations
        node.AddNode(ConfigurationUIFactory.CreateCheckboxOption("Animations enabled",
            () => getConfig().AnimationsEnabled,
            value => {
                getConfig().AnimationsEnabled = value;
                onChanged?.Invoke();
            }
        ));

        node.AddDummy(checkBoxHeight);

        return node;
    }
}
