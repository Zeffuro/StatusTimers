using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes;
using System;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class FunctionalSectionNode : VerticalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly SectionHeaderNode _sectionHeaderNode;

    public FunctionalSectionNode(Func<StatusTimerOverlayConfig> getConfig, NodeKind kind) {
        _getConfig = getConfig;

        _sectionHeaderNode = new SectionHeaderNode("Functional Settings");
        AddNode(_sectionHeaderNode);

        if (kind == NodeKind.Combined)
        {
            // Hide permanent statuses
            AddNode(new CheckboxOptionNode {
                LabelText = "Hide permanent statuses",
                IsChecked = !getConfig().ShowPermaIcons,
                OnClick = isChecked => getConfig().ShowPermaIcons = !isChecked
            });

            // Show food/potion name
            AddNode(new CheckboxOptionNode {
                LabelText = "Show food or potion name instead of Well Fed/Medicated",
                IsChecked = getConfig().StatusAsItemName,
                OnClick = isChecked => getConfig().StatusAsItemName = isChecked
            });

            // Allow dismissing status by right-clicking the status icon
            AddNode(new CheckboxOptionNode {
                LabelText = "Allow dismissing status by right-clicking the status icon.",
                IsChecked = getConfig().AllowDismissStatus,
                OnClick = isChecked => getConfig().AllowDismissStatus = isChecked
            });
        }

        // MultiDoT overlay settings
        if (kind == NodeKind.MultiDoT)
        {
            // Allow targeting the enemy by clicking the status icon
            AddNode(new CheckboxOptionNode {
                LabelText = "Allow targeting the enemy by clicking the status icon.",
                IsChecked = getConfig().AllowTargetActor,
                OnClick = isChecked => getConfig().AllowTargetActor = isChecked
            });
        }

        RecalculateLayout();
    }
}
