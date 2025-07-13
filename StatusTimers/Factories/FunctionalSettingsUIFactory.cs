using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Factories;
using StatusTimers.Helpers;

namespace StatusTimers.Factories;

public static class FunctionalSettingsUIFactory
{
    public static VerticalListNode Create(
        Func<StatusTimerOverlayConfig> getConfig,
        NodeKind kind,
        Action? onChanged = null,
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
            AlignmentType = AlignmentType.Left,
            Text = "Functional Settings"
        });

        // Combined overlay settings
        if (kind == NodeKind.Combined)
        {
            // Hide permanent statuses
            node.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                "Hide permanent statuses",
                () => !getConfig().ShowPermaIcons,
                isChecked => { getConfig().ShowPermaIcons = !isChecked; onChanged?.Invoke(); }
            ));

            // Show food/potion name
            node.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                "Show food or potion name instead of Well Fed/Medicated.",
                () => getConfig().StatusAsItemName,
                isChecked => { getConfig().StatusAsItemName = isChecked; onChanged?.Invoke(); }
            ));

            // Allow dismissing status by right-clicking the status icon
            node.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                "Allow dismissing status by right-clicking the status icon.",
                () => getConfig().AllowDismissStatus,
                isChecked => { getConfig().AllowDismissStatus = isChecked; onChanged?.Invoke(); }
            ));
        }

        // MultiDoT overlay settings
        if (kind == NodeKind.MultiDoT)
        {
            // Allow targeting the enemy by clicking the status icon
            node.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                "Allow targeting the enemy by clicking the status icon.",
                () => getConfig().AllowTargetActor,
                isChecked => { getConfig().AllowTargetActor = isChecked; onChanged?.Invoke(); }
            ));
        }

        node.AddDummy(checkBoxHeight);

        return node;
    }
}
