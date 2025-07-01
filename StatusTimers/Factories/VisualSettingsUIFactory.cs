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
    public static VerticalListNode<NodeBase> Create(
        StatusTimerOverlay<StatusKey> overlay,
        StatusTimerOverlayConfig config,
        Action? onChanged = null,
        float optionOffset = 18,
        float checkBoxHeight = 16)
    {
        var node = new VerticalListNode<NodeBase>
        {
            IsVisible = true,
            ItemVerticalSpacing = 3,
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
        node.AddDummy(new ResNode(), checkBoxHeight);

        // Scale + Horizontal Padding
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
            ConfigurationUIFactory.CreateLabeledNumericOption("Horizontal Padding",
                () => config.StatusHorizontalPadding,
                value => { config.StatusHorizontalPadding = value; onChanged?.Invoke(); }
            ),
            checkBoxHeight
        ));

        node.AddNode();

        // Scale slider + Vertical Padding
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateSliderOption(
                5,
                200,
                5,
                () => config.ScaleInt,
                value => { config.ScaleInt = value; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateLabeledNumericOption("Vertical Padding",
                () => config.StatusVerticalPadding,
                value => { config.StatusVerticalPadding = value; onChanged?.Invoke(); }
            ),
            30
        ));

        node.AddDummy(new ResNode(), checkBoxHeight);

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
            Text = $"Statuses per {(config.FillRowsFirst ? "row" : "column")}"
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
                () => config.ItemsPerLine,
                value => { config.ItemsPerLine = value; onChanged?.Invoke(); }
            ),
            ConfigurationUIFactory.CreateSliderOption(
                1,
                30,
                1,
                () => config.MaxStatuses,
                value => { config.MaxStatuses = value; onChanged?.Invoke(); }
            ),
            30
        ));

        // Fill columns first + Grow direction dropdown
        node.AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
            ConfigurationUIFactory.CreateCheckboxOption("Fill columns first",
                () => !config.FillRowsFirst,
                isChecked =>
                {
                    config.FillRowsFirst = !isChecked;
                    statusPerLineNode.Text = $"Statuses per {(config.FillRowsFirst ? "row" : "column")}";
                    onChanged?.Invoke();
                }),
            ConfigurationUIFactory.CreateLabeledDropdown(
                "Grow direction",
                () => config.GrowDirection,
                value => { config.GrowDirection = value; onChanged?.Invoke(); },
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
            () => config.AnimationsEnabled,
            value => {
                config.AnimationsEnabled = value;
                onChanged?.Invoke();
            }
        ));

        node.AddDummy(new ResNode(), checkBoxHeight);

        return node;
    }
}
