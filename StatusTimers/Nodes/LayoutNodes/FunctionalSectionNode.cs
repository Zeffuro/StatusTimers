using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class FunctionalSectionNode : TabbedVerticalListNode {
    public FunctionalSectionNode(Func<StatusTimerOverlayConfig> getConfig, NodeKind kind) {
        FitContents = true;
        ItemSpacing = 2;

        SectionHeaderNode sectionHeaderNode = new("Functional Settings");

        AddNode(sectionHeaderNode);

        AddTab(1);
        var nodes = new List<NodeBase> { new ResNode{Height = 2} };

        // Hide statuses above a certain max duration
        var secondsNode = new LabeledNumericOptionNode("",
            () => getConfig().HideStatusAboveSeconds,
            value => { getConfig().HideStatusAboveSeconds = value; }
        ) {
            IsVisible = getConfig().HideStatusAboveSecondsEnabled,
            Y = -4,
        };

        // Hide statuses above a certain max enabled
        nodes.Add(new TwoOptionsRowNode(new CheckboxOptionNode {
            String = "Hide statuses above a certain max duration",
            IsChecked = getConfig().HideStatusAboveSecondsEnabled,
            OnClick = isChecked => {
                getConfig().HideStatusAboveSecondsEnabled = isChecked;
                secondsNode.IsVisible = isChecked;
            }
        }, secondsNode, 20));

        // Hide statuses under a certain max duration
        var underSecondsNode = new LabeledNumericOptionNode("",
            () => getConfig().HideStatusUnderSeconds,
            value => { getConfig().HideStatusUnderSeconds = value; }
        ) {
            IsVisible = getConfig().HideStatusUnderSecondsEnabled,
            Y = -4,
        };

        // Hide statuses above a certain max enabled
        nodes.Add(new TwoOptionsRowNode(new CheckboxOptionNode {
            String = "Hide statuses under a certain max duration",
            IsChecked = getConfig().HideStatusUnderSecondsEnabled,
            OnClick = isChecked => {
                getConfig().HideStatusUnderSecondsEnabled = isChecked;
                underSecondsNode.IsVisible = isChecked;
            }
        }, underSecondsNode, 20));

        if (kind is NodeKind.Combined or NodeKind.Buffs or NodeKind.Debuffs)
        {
            // Hide statuses that are not applied by the player
            nodes.Add(new CheckboxOptionNode {
                String = "Show self applied statuses only",
                IsChecked = getConfig().SelfAppliedStatusesOnly,
                OnClick = isChecked => getConfig().SelfAppliedStatusesOnly = isChecked
            });

            // Hide permanent statuses
            nodes.Add(new CheckboxOptionNode {
                String = "Hide permanent statuses",
                IsChecked = !getConfig().ShowPermaIcons,
                OnClick = isChecked => getConfig().ShowPermaIcons = !isChecked
            });

            // Show food/potion name
            nodes.Add(new CheckboxOptionNode {
                String = "Show food or potion name instead of Well Fed/Medicated",
                IsChecked = getConfig().StatusAsItemName,
                OnClick = isChecked => getConfig().StatusAsItemName = isChecked
            });

            // Allow dismissing status by right-clicking the status icon
            nodes.Add(new CheckboxOptionNode {
                String = "Allow dismissing status by right-clicking the status icon.",
                IsChecked = getConfig().AllowDismissStatus,
                OnClick = isChecked => getConfig().AllowDismissStatus = isChecked
            });
        }

        // MultiDoT overlay settings
        if (kind == NodeKind.MultiDoT)
        {
            // Allow targeting the enemy by clicking the status icon
            nodes.Add(new CheckboxOptionNode {
                String = "Allow targeting the enemy by clicking the status icon.",
                IsChecked = getConfig().AllowTargetActor,
                OnClick = isChecked => getConfig().AllowTargetActor = isChecked
            });
        }

        nodes.Add(new CheckboxOptionNode {
            String = "Only show when in combat",
            IsChecked = getConfig().InCombatOnly,
            OnClick = isChecked => getConfig().InCombatOnly = isChecked
        });

        AddNode(nodes);
        RecalculateLayout();
    }
}
