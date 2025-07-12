using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.TabBar;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Factories;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;

    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _filterSectionNodes = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private TabBarNode _tabBar;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        _configScrollingAreas.Clear();
        _configLists.Clear();
        _filterSectionNodes.Clear();
        _tabBar = null;

        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode node) in _configScrollingAreas) {
            node.IsVisible = k == kind;
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>().ToList()
            .ForEach(kind => {
                var overlay = GetOverlayByKind(kind);
                if (overlay != null) {
                    overlay.IsPreviewEnabled = false;
                    overlay.IsLocked = true;
                }
            });
    }

    private void SetupOptions() {
        _tabBar = new TabBarNode {
            Position = ContentStartPosition,
            Size = ContentSize with { Y = 24 },
            Height = 24,
            IsVisible = true
        };

        NodeKind[] nodeKinds = Enum.GetValues<NodeKind>();

        foreach ((NodeKind kind, int _) in nodeKinds.Select((kind, index) => (kind, index))) {
            StatusTimerOverlay<StatusKey>? overlay = GetOverlayByKind(kind);

            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));

            if (overlay == null || overlay.OverlayConfig == null) {
                continue;
            }

            _configScrollingAreas[kind] = new ScrollingAreaNode {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + _tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - _tabBar.Height,
                ContentHeight = 1500.0f,
                IsVisible = false
            };
            NativeController.AttachNode(_configScrollingAreas[kind], this);

            _configLists[kind] = new VerticalListNode<NodeBase> {
                Height = 0,
                Width = _configScrollingAreas[kind].ContentNode.Width,
                IsVisible = true,
                FitContents = true,
                ItemSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            var mainSettingsGroup = new VerticalListNode<NodeBase> {
                IsVisible = overlay.IsVisible,
                Width = _configScrollingAreas[kind].ContentNode.Width,
                FitContents = true,
                ItemSpacing = 3
            };

            _configLists[kind].AddNode(
                ImportExportResetUIFactory.Create(() => overlay, () => overlay.OverlayConfig, kind,
                    onConfigChanged: () => { },
                    closeWindow: Close)
            );

            var enabledCheckbox = ConfigurationUIFactory.CreateCheckboxOption(
                "Enabled",
                () => overlay.OverlayConfig.Enabled,
                isChecked => {
                    overlay.OverlayConfig.Enabled = isChecked;
                    ToggleEnabled(overlay, mainSettingsGroup, kind, isChecked);
                }
            );

            _configLists[kind].AddNode(enabledCheckbox);

            mainSettingsGroup.AddDummy(new ResNode(), CheckBoxHeight);

            // Visual Settings
            mainSettingsGroup.AddNode(
                VisualSettingsUIFactory.Create(
                    overlay,
                    () => overlay.OverlayConfig,
                    onChanged: () => { },
                    optionOffset: OptionOffset,
                    checkBoxHeight: CheckBoxHeight
                )
            );

            // Icon Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "icon",
                    overlay.OverlayConfig.Icon,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Name Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "status name",
                    overlay.OverlayConfig.Name,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Name), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Time Remaining Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "time remaining",
                    overlay.OverlayConfig.Timer,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Timer), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Progress Bar Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "progressbar",
                    overlay.OverlayConfig.Progress,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Progress), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Actor Name Settings
            if (kind == NodeKind.MultiDoT) {
                mainSettingsGroup.AddNode(
                    NodeLayoutUIFactory.CreateNodeLayoutSection(
                        "enemy name",
                        overlay.OverlayConfig.Actor,
                        overlayManager,
                        onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Actor), updateNodes: true),
                        onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                    )
                );

                mainSettingsGroup.AddNode(ConfigurationUIFactory.CreateCheckboxOption("Show enemy letter",
                    () => overlay.OverlayConfig.ShowActorLetter,
                    isChecked => overlay.OverlayConfig.ShowActorLetter = isChecked));
            }

            mainSettingsGroup.AddDummy(new ResNode(), CheckBoxHeight);

            // Functional Settings
            mainSettingsGroup.AddNode(
                FunctionalSettingsUIFactory.Create(
                    () => overlay.OverlayConfig,
                    kind,
                    onChanged: () => { },
                    checkBoxHeight: CheckBoxHeight
                )
            );

            // Sorting Priority Settings
            mainSettingsGroup.AddNode(
                SortUIFactory.CreateSortPrioritySection(() => overlay.OverlayConfig, kind)
            );

            // Filtering Settings
            void UpdateFilterSection() {
                var oldNode = _filterSectionNodes.ContainsKey(kind) ? _filterSectionNodes[kind] : null;
                var newNode = FilterUIFactory.CreateFilterSection(() => overlay.OverlayConfig,
                    () => {
                        GlobalServices.Logger.Info("Filter section changed");
                        UpdateFilterSection();
                        RecalculateAllLayouts(mainSettingsGroup, kind, true);
                    },
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind, true)
                );
                _filterSectionNodes[kind] = newNode;
                if (oldNode != null) {
                    mainSettingsGroup.RemoveNode(oldNode);
                }

                mainSettingsGroup.AddNode(newNode);
            }
            UpdateFilterSection();

            _configScrollingAreas[kind].ContentHeight = _configLists[kind].Height;
            _configLists[kind].AddNode(mainSettingsGroup);

            mainSettingsGroup.RecalculateLayout();
            _configLists[kind].RecalculateLayout();
            _configScrollingAreas[kind].ContentHeight = _configLists[kind].Height;
        }

        NativeController.AttachNode(_tabBar, this);
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    #region Helper Methods

    private StatusTimerOverlay<StatusKey>? GetOverlayByKind(NodeKind kind) {
        return (kind switch {
            NodeKind.Combined => overlayManager?.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager?.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported NodeKind")
        });
    }

    private void RecalculateAllLayouts(VerticalListNode<NodeBase> group, NodeKind kind, bool scrollToBottom = false) {
        group.RecalculateLayout();
        _configLists[kind].RecalculateLayout();
        _configScrollingAreas[kind].ContentHeight = _configLists[kind].Height;
        if (scrollToBottom) {
            _configScrollingAreas[kind].ScrollPosition = (int)_configScrollingAreas[kind].ContentHeight;
        }
    }

    private void ToggleEnabled(StatusTimerOverlay<StatusKey>? overlay, VerticalListNode<NodeBase> group, NodeKind kind, bool isChecked) {
        if (overlay != null) {
            overlay.IsVisible = isChecked;
            group.IsVisible = isChecked;
            group.Height = isChecked ? -1 : 0;
            group.RecalculateLayout();
            _configLists[kind].RecalculateLayout();
            _configScrollingAreas[kind].ContentHeight = _configLists[kind].Height;
        }
    }

    #endregion
}
