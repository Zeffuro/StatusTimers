using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
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

    private readonly Dictionary<NodeKind, ScrollingAreaNode<VerticalListNode>?>
        _configScrollingAreas = new();

    private readonly Dictionary<NodeKind, FilterSectionNode> _filterSectionNodes = new();
    private TabBarNode? _tabBar;

    private bool _isRecalculating;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        _configScrollingAreas.Clear();
        _configLists.Clear();
        _filterSectionNodes.Clear();
        _tabBar = null;

        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode<VerticalListNode>? node) in
                 _configScrollingAreas) {
            if (node != null) {
                node.IsVisible = k == kind;
            }
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (_filterSectionNodes.Count == 0) {
            return;
        }

        foreach (var filterNode in _filterSectionNodes.Values) {
            filterNode.OnUpdate();
        }
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        overlayManager.CloseColorPicker();
        foreach (var kind in Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>()) {
            var overlay = GetOverlayByKind(kind);
            if (overlay == null) {
                continue;
            }

            overlay.IsPreviewEnabled = false;
            overlay.IsLocked = true;
        }
    }

    private void SetupOptions() {
        _tabBar = new TabBarNode {
            Position = ContentStartPosition,
            Size = ContentSize with { Y = 24 },
            Height = 24,
            IsVisible = true
        };
        _tabBar.AttachNode(this);

        var nodeKinds = Enum.GetValues<NodeKind>();

        foreach (var kind in nodeKinds) {
            var overlay = GetOverlayByKind(kind);

            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind),
                isEnabled: overlay?.OverlayConfig != null);

            if (overlay?.OverlayConfig == null) {
                continue;
            }

            overlay.IsVisible = overlay.OverlayConfig.Enabled;

            var configScrollingArea = new ScrollingAreaNode<VerticalListNode> {
                Position =
                    ContentStartPosition + new System.Numerics.Vector2(0, _tabBar.Height),
                Size = ContentSize with { Y = ContentSize.Y - _tabBar.Height },
                ContentHeight = 1500.0f,
                ScrollSpeed = 50,
                IsVisible = false
            };
            configScrollingArea.AttachNode(this);
            _configScrollingAreas[kind] = configScrollingArea;

            var list = configScrollingArea.ContentNode;
            list.IsVisible = true;
            list.FitContents = true;
            list.ItemSpacing = 3;
            list.Width = configScrollingArea.Width;
            list.Height = 0;
            _configLists[kind] = list;

            // main group respects Enabled at startup
            var mainSettingsGroup = new VerticalListNode {
                IsVisible = overlay.OverlayConfig.Enabled,
                Height = overlay.OverlayConfig.Enabled ? -1 : 0,
                Width = list.Width,
                FitContents = true,
                ItemSpacing = 3
            };

            list.AddNode(
                new ImportExportResetNode(
                    () => overlay,
                    () => overlay.OverlayConfig,
                    kind,
                    onConfigChanged: () => { },
                    closeWindow: Close
                )
            );

            var enabledCheckbox = new CheckboxOptionNode {
                String = "Enabled",
                IsChecked = overlay.OverlayConfig.Enabled,
                OnClick = isChecked => {
                    overlay.OverlayConfig.Enabled = isChecked;
                    ToggleEnabled(overlay, mainSettingsGroup, kind, isChecked);
                }
            };
            list.AddNode(enabledCheckbox);

            mainSettingsGroup.AddDummy(CheckBoxHeight);

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

            var defaultConfig = new StatusTimerOverlayConfig(kind);

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "background",
                    overlay.OverlayConfig.Background,
                    defaultConfig.Background,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Background),
                            updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "icon",
                    overlay.OverlayConfig.Icon,
                    defaultConfig.Icon,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon),
                            updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "status name",
                    overlay.OverlayConfig.Name,
                    defaultConfig.Name,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Name),
                            updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            var timerFormatNode = new StatusTimerFormatRowNode(
                () => overlay.OverlayConfig,
                onChanged: _ =>
                    overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon),
                        updateNodes: true)
            );

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "time remaining",
                    overlay.OverlayConfig.Timer,
                    defaultConfig.Timer,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Timer),
                            updateNodes: true),
                    onToggled: () => {
                        timerFormatNode.IsVisible = overlay.OverlayConfig.Timer.IsVisible;
                        RecalculateAllLayouts(mainSettingsGroup, kind);
                    })
            );

            mainSettingsGroup.AddNode(timerFormatNode);

            mainSettingsGroup.AddNode(
                new NodeLayoutSectionNode(
                    "progressbar",
                    overlay.OverlayConfig.Progress,
                    defaultConfig.Progress,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Progress),
                            updateNodes: true),
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            if (kind == NodeKind.MultiDoT) {
                mainSettingsGroup.AddNode(
                    new NodeLayoutSectionNode(
                        "enemy name",
                        overlay.OverlayConfig.Actor,
                        defaultConfig.Actor,
                        overlayManager,
                        onChanged: () =>
                            overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Actor),
                                updateNodes: true),
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

            mainSettingsGroup.AddNode(
                new FunctionalSectionNode(() => overlay.OverlayConfig, kind) {
                    IsVisible = true,
                    Width = 600,
                    Height = 120,
                    ItemSpacing = 4,
                    FitContents = true,
                });

            mainSettingsGroup.AddNode(new SortingSectionNode(() => overlay.OverlayConfig, kind) {
                IsVisible = true,
                Width = 600,
                Height = 100,
                ItemSpacing = 2,
                FitContents = true
            });

            _filterSectionNodes[kind] = new FilterSectionNode(
                () => overlay.OverlayConfig,
                onChanged: () => {
                    overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.FilterList));
                    RecalculateAllLayouts(mainSettingsGroup, kind, true);
                }
            ) {
                IsVisible = true,
                Width = 600,
                Height = 100,
                ItemSpacing = 4,
                FitContents = true,
            };
            mainSettingsGroup.AddNode(_filterSectionNodes[kind]);

            list.AddNode(mainSettingsGroup);

            list.RecalculateLayout();
            configScrollingArea.ContentHeight = list.Height;

            RecalculateAllLayouts(mainSettingsGroup, kind);
        }

        var firstArea = _configScrollingAreas.Values.FirstOrDefault(node => node != null);
        if (firstArea != null) {
            firstArea.IsVisible = true;
        }
    }

    private StatusTimerOverlayNode<StatusKey>? GetOverlayByKind(NodeKind kind) {
        return kind switch {
            NodeKind.Combined => overlayManager.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
                "Unsupported NodeKind")
        };
    }

    private void RecalculateAllLayouts(VerticalListNode group, NodeKind kind,
        bool scrollToBottom = false) {
        if (_isRecalculating) {
            return;
        }

        _isRecalculating = true;

        foreach (var node in group.Nodes) {
            if (node is LayoutListNode layoutNode) {
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

    private void ToggleEnabled(StatusTimerOverlayNode<StatusKey>? overlay, VerticalListNode group,
        NodeKind kind, bool isChecked) {
        if (overlay == null) {
            return;
        }

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
