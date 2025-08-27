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

        // Hide statuses above a certain max duration
        var secondsNode = new LabeledNumericOptionNode("",
            () => getConfig().HideStatusAboveSeconds,
            value => { getConfig().HideStatusAboveSeconds = value; }
        ) {
            IsVisible = getConfig().HideStatusAboveSecondsEnabled,
            Y = -4,
        };

        // Hide statuses above a certain max enabled
        AddNode(new TwoOptionsRowNode(new CheckboxOptionNode {
            String = "Hide statuses above a certain max duration",
            IsChecked = getConfig().HideStatusAboveSecondsEnabled,
            OnClick = isChecked => {
                getConfig().HideStatusAboveSecondsEnabled = isChecked;
                secondsNode.IsVisible = isChecked;
            }
        }, secondsNode, 16));

        // Hide statuses under a certain max duration
        var underSecondsNode = new LabeledNumericOptionNode("",
            () => getConfig().HideStatusUnderSeconds,
            value => { getConfig().HideStatusUnderSeconds = value; }
        ) {
            IsVisible = getConfig().HideStatusUnderSecondsEnabled,
            Y = -4,
        };

        // Hide statuses above a certain max enabled
        AddNode(new TwoOptionsRowNode(new CheckboxOptionNode {
            String = "Hide statuses under a certain max duration",
            IsChecked = getConfig().HideStatusUnderSecondsEnabled,
            OnClick = isChecked => {
                getConfig().HideStatusUnderSecondsEnabled = isChecked;
                underSecondsNode.IsVisible = isChecked;
            }
        }, underSecondsNode, 16));

        if (kind == NodeKind.Combined)
        {
            // Hide statuses that are not applied by the player
            AddNode(new CheckboxOptionNode {
                String = "Show self applied statuses only",
                IsChecked = getConfig().SelfAppliedStatusesOnly,
                OnClick = isChecked => getConfig().SelfAppliedStatusesOnly = isChecked
            });

            // Hide permanent statuses
            AddNode(new CheckboxOptionNode {
                String = "Hide permanent statuses",
                IsChecked = !getConfig().ShowPermaIcons,
                OnClick = isChecked => getConfig().ShowPermaIcons = !isChecked
            });

            // Show food/potion name
            AddNode(new CheckboxOptionNode {
                String = "Show food or potion name instead of Well Fed/Medicated",
                IsChecked = getConfig().StatusAsItemName,
                OnClick = isChecked => getConfig().StatusAsItemName = isChecked
            });

            // Allow dismissing status by right-clicking the status icon
            AddNode(new CheckboxOptionNode {
                String = "Allow dismissing status by right-clicking the status icon.",
                IsChecked = getConfig().AllowDismissStatus,
                OnClick = isChecked => getConfig().AllowDismissStatus = isChecked
            });
        }

        // MultiDoT overlay settings
        if (kind == NodeKind.MultiDoT)
        {
            // Allow targeting the enemy by clicking the status icon
            AddNode(new CheckboxOptionNode {
                String = "Allow targeting the enemy by clicking the status icon.",
                IsChecked = getConfig().AllowTargetActor,
                OnClick = isChecked => getConfig().AllowTargetActor = isChecked
            });
        }

        RecalculateLayout();
    }
}
