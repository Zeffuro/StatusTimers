using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.TabBar;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float CheckBoxHeight = 16;

    private readonly Dictionary<NodeKind, VerticalListNode> _configLists = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode<ResNode>?> _configScrollingAreas = new();
    private readonly Dictionary<NodeKind, FilterSectionNode> _filterSectionNodes = new();
    private TabBarNode? _tabBar;

    private bool _isRecalculating;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        _configScrollingAreas.Clear();
        _configLists.Clear();
        _tabBar = null;

        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode<ResNode>? node) in _configScrollingAreas) {
            if (node != null) {
                node.IsVisible = k == kind;
            }
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (_filterSectionNodes.Count > 0) {
            foreach (var filterNode in _filterSectionNodes.Values) {
                filterNode.OnUpdate();
            }
        }
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

            _configScrollingAreas[kind] = new ScrollingAreaNode<ResNode> {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + _tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - _tabBar.Height,
                ContentHeight = 1500.0f,
                ScrollSpeed = 50,
                IsVisible = false
            };

            var configScrollingArea = _configScrollingAreas[kind];
            if (configScrollingArea == null) {
                return;
            }

            AttachNode(configScrollingArea);

            _configLists[kind] = new VerticalListNode {
                Height = 0,
                Width = configScrollingArea.Width,
                IsVisible = true,
                FitContents = true,
                ItemSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], configScrollingArea.ContentNode);

            var mainSettingsGroup = new VerticalListNode {
                IsVisible = overlay.IsVisible,
                Height = 100,
                Width = configScrollingArea.ContentNode.Width,
                FitContents = true,
                ItemSpacing = 3
            };

            _configLists[kind].AddNode(
                new ImportExportResetNode(() => overlay, () => overlay.OverlayConfig, kind, onConfigChanged: () => { }, closeWindow: Close)
            );

            var enabledCheckbox = new CheckboxOptionNode {
                String = "Enabled",
                IsChecked = overlay.OverlayConfig.Enabled,
                OnClick = isChecked => {
                    overlay.OverlayConfig.Enabled = isChecked;
                    ToggleEnabled(overlay, mainSettingsGroup, kind, isChecked);
                }
            };

            _configLists[kind].AddNode(enabledCheckbox);

            mainSettingsGroup.AddDummy(CheckBoxHeight);

            // Visual Settings
            mainSettingsGroup.AddNode(
                new VisualSectionNode(
                    overlay,
                    () => overlay.OverlayConfig
                ) {
                    IsVisible = true,
                    Height = 280,
                    Width = 600,
                    ItemSpacing = 4,
                    FitContents = true
                }
            );

            // Background Settings
            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "background",
                    overlay.OverlayConfig.Background,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Background), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Icon Settings
            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "icon",
                    overlay.OverlayConfig.Icon,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Name Settings
            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "status name",
                    overlay.OverlayConfig.Name,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Name), updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Time Remaining Settings
            var timerFormatNode = new StatusTimerFormatRowNode(() => overlay.OverlayConfig,
                onChanged: _ =>
                    overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon), updateNodes: true)
            );

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "time remaining",
                    overlay.OverlayConfig.Timer,
                    overlayManager,
                    onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Timer), updateNodes: true),
                    onToggled: () => {
                        timerFormatNode.IsVisible = overlay.OverlayConfig.Timer.IsVisible;
                        RecalculateAllLayouts(mainSettingsGroup, kind);
                    })
            );

            mainSettingsGroup.AddNode(timerFormatNode);

            // Progress Bar Settings
            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
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
                    new NodeLayoutSectionNode(
                        "enemy name",
                        overlay.OverlayConfig.Actor,
                        overlayManager,
                        onChanged: () => overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Actor), updateNodes: true),
                        onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                    )
                );

                mainSettingsGroup.AddNode(new CheckboxOptionNode {
                    String = "Show enemy letter",
                    IsChecked = overlay.OverlayConfig.ShowActorLetter,
                    OnClick = isChecked => overlay.OverlayConfig.ShowActorLetter = isChecked
                });
            }

            mainSettingsGroup.AddDummy(CheckBoxHeight);

            // Functional Settings
            mainSettingsGroup.AddNode(new FunctionalSectionNode(() => overlay.OverlayConfig, kind) {
                IsVisible = true,
                Width = 600,
                Height = 120,
                ItemSpacing = 4,
                FitContents = true,
            });

            // Sorting Priority Settings
            mainSettingsGroup.AddNode(new SortingSectionNode(() => overlay.OverlayConfig, kind)
            {
                IsVisible = true,
                Width = 600,
                Height = 100,
                ItemSpacing = 2,
                FitContents = true
            });

            // Filtering Settings
            _filterSectionNodes[kind] = new FilterSectionNode(() => overlay.OverlayConfig, onChanged: () => {
                RecalculateAllLayouts(mainSettingsGroup, kind, true);
            }) {
                IsVisible = true,
                Width = 600,
                Height = 100,
                ItemSpacing = 4,
                FitContents = true,
            };
            mainSettingsGroup.AddNode(_filterSectionNodes[kind]);
            _configLists[kind].AddNode(mainSettingsGroup);

            _configLists[kind].RecalculateLayout();
            configScrollingArea.ContentHeight = _configLists[kind].Height;

            RecalculateAllLayouts(mainSettingsGroup, kind);
        }

        AttachNode(_tabBar);
        var scrollingAreaNode = _configScrollingAreas.First().Value;
        if (scrollingAreaNode != null) {
            scrollingAreaNode.IsVisible = true;
        }
    }

    #region Helper Methods

    private StatusTimerOverlay<StatusKey>? GetOverlayByKind(NodeKind kind) {
        return (kind switch {
            NodeKind.Combined => overlayManager.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported NodeKind")
        });
    }

    private void RecalculateAllLayouts(VerticalListNode group, NodeKind kind, bool scrollToBottom = false) {
        if (_isRecalculating) {
            return;
        }

        _isRecalculating = true;

        foreach (var node in group.Nodes) {
            if(node is LayoutListNode layoutNode) {
                layoutNode.RecalculateLayout();
            }
        }

        group.RecalculateLayout();
        _configLists[kind].RecalculateLayout();

        var configScrollingArea = _configScrollingAreas[kind];
        if (configScrollingArea == null) {
            _isRecalculating = false;
            return;
        }

        configScrollingArea.ContentHeight = _configLists[kind].Height;
        if (scrollToBottom) {
            configScrollingArea.ScrollPosition = (int)configScrollingArea.ContentHeight;
        }

        _isRecalculating = false;
    }

    private void ToggleEnabled(StatusTimerOverlay<StatusKey>? overlay, VerticalListNode group, NodeKind kind, bool isChecked) {
        if (overlay != null) {
            overlay.IsVisible = isChecked;
            group.IsVisible = isChecked;
            group.Height = isChecked ? -1 : 0;
            group.RecalculateLayout();
            _configLists[kind].RecalculateLayout();
            var configScrollingArea = _configScrollingAreas[kind];
            if (configScrollingArea == null) {
                return;
            }
            configScrollingArea.ContentHeight = _configLists[kind].Height;
        }
    }

    #endregion
}
