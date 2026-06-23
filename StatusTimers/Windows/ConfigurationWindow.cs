using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
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

    private readonly Dictionary<NodeKind, ConfigVerticalListNode> _configLists = new();

    private readonly Dictionary<NodeKind, ScrollingNode<ConfigVerticalListNode>?>
        _configScrollingAreas = new();

    private readonly Dictionary<NodeKind, FilterSectionNode> _filterSectionNodes = new();
    private readonly HashSet<NodeKind> _pendingLayoutKinds = [];
    private TabBarNode? _tabBar;

    private bool _isRecalculating;

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        _configScrollingAreas.Clear();
        _configLists.Clear();
        _filterSectionNodes.Clear();
        _pendingLayoutKinds.Clear();
        _tabBar = null;

        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingNode<ConfigVerticalListNode>? node) in
                 _configScrollingAreas) {
            if (node != null) {
                node.IsVisible = k == kind;
            }
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        foreach (var filterNode in _filterSectionNodes.Values) {
            filterNode.OnUpdate();
        }

        FlushPendingLayoutRecalculations();
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

            var configScrollingArea = new ScrollingNode<ConfigVerticalListNode> {
                Position =
                    ContentStartPosition + new System.Numerics.Vector2(0, _tabBar.Height),
                ContentNode = {
                    FitContents = true,
                    ItemSpacing = 3
                },
                Size = ContentSize with { Y = ContentSize.Y - _tabBar.Height },
                ScrollSpeed = 50,
                IsVisible = false
            };
            configScrollingArea.AttachNode(this);
            _configScrollingAreas[kind] = configScrollingArea;

            var list = configScrollingArea.ContentNode;
            list.IsVisible = true;
            list.Width = configScrollingArea.Width;
            list.Height = 0;
            _configLists[kind] = list;
            var listNodes = new List<NodeBase>();

            // main group respects Enabled at startup
            var mainSettingsGroup = new ConfigVerticalListNode {
                IsVisible = overlay.OverlayConfig.Enabled,
                Height = 0,
                Width = list.Width,
                FitContents = true,
                ItemSpacing = 3
            };
            var mainSettingsNodes = new List<NodeBase>();

            listNodes.Add(
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
            listNodes.Add(enabledCheckbox);

            mainSettingsNodes.Add(new ResNode { Width = CheckBoxHeight, Height = CheckBoxHeight });

            mainSettingsNodes.Add(
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

            mainSettingsNodes.Add(
                new NodeLayoutSectionNode(
                    "background",
                    overlay.OverlayConfig.Background,
                    defaultConfig.Background,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Background),
                            updateNodes: true),
                    onToggled: () => QueueRecalculateAllLayouts(kind)
                )
            );

            mainSettingsNodes.Add(
                new NodeLayoutSectionNode(
                    "icon",
                    overlay.OverlayConfig.Icon,
                    defaultConfig.Icon,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon),
                            updateNodes: true),
                    onToggled: () => QueueRecalculateAllLayouts(kind)
                )
            );

            mainSettingsNodes.Add(
                new NodeLayoutSectionNode(
                    "status name",
                    overlay.OverlayConfig.Name,
                    defaultConfig.Name,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Name),
                            updateNodes: true),
                    onToggled: () => QueueRecalculateAllLayouts(kind)
                )
            );

            var timerFormatNode = new StatusTimerFormatRowNode(
                () => overlay.OverlayConfig,
                onChanged: _ =>
                    overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Icon),
                        updateNodes: true)
            );

            mainSettingsNodes.Add(
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
                        QueueRecalculateAllLayouts(kind);
                    })
            );

            mainSettingsNodes.Add(timerFormatNode);

            mainSettingsNodes.Add(
                new NodeLayoutSectionNode(
                    "progressbar",
                    overlay.OverlayConfig.Progress,
                    defaultConfig.Progress,
                    overlayManager,
                    onChanged: () =>
                        overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Progress),
                            updateNodes: true),
                    onToggled: () => QueueRecalculateAllLayouts(kind)
                )
            );

            if (kind == NodeKind.MultiDoT) {
                mainSettingsNodes.Add(
                    new NodeLayoutSectionNode(
                        "enemy name",
                        overlay.OverlayConfig.Actor,
                        defaultConfig.Actor,
                        overlayManager,
                        onChanged: () =>
                            overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.Actor),
                                updateNodes: true),
                        onToggled: () => QueueRecalculateAllLayouts(kind)
                    )
                );

                mainSettingsNodes.Add(new CheckboxOptionNode {
                    String = "Show enemy letter",
                    IsChecked = overlay.OverlayConfig.ShowActorLetter,
                    OnClick = isChecked => overlay.OverlayConfig.ShowActorLetter = isChecked
                });
            }

            mainSettingsNodes.Add(new ResNode { Width = CheckBoxHeight, Height = CheckBoxHeight });

            mainSettingsNodes.Add(
                new FunctionalSectionNode(() => overlay.OverlayConfig, kind) {
                    IsVisible = true,
                    Width = 600,
                    Height = 120,
                    ItemSpacing = 4,
                    FitContents = true,
                });

            mainSettingsNodes.Add(new SortingSectionNode(() => overlay.OverlayConfig, kind) {
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
                    QueueRecalculateAllLayouts(kind);
                }
            ) {
                IsVisible = true,
                Width = 600,
                Height = 100,
                ItemSpacing = 4,
                FitContents = true,
            };
            mainSettingsNodes.Add(_filterSectionNodes[kind]);

            mainSettingsGroup.AddNode(mainSettingsNodes);
            listNodes.Add(mainSettingsGroup);
            list.AddNode(listNodes);

            RecalculateAllLayouts(kind);
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
            NodeKind.Buffs => overlayManager.PlayerBuffsOverlayInstance,
            NodeKind.Debuffs => overlayManager.PlayerDebuffsOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
                "Unsupported NodeKind")
        };
    }

    private void RecalculateAllLayouts(NodeKind kind) {
        if (_isRecalculating) {
            _pendingLayoutKinds.Add(kind);
            return;
        }

        try {
            _isRecalculating = true;
            if (!_configScrollingAreas.TryGetValue(kind, out var configScrollingArea) ||
                configScrollingArea == null) {
                return;
            }

            ConfigLayoutRecalculation.RecalculateBottomUp(configScrollingArea.ContentNode);
            ConfigLayoutRecalculation.UpdateScrollParams(configScrollingArea);
        } finally {
            _isRecalculating = false;
        }
    }

    private void QueueRecalculateAllLayouts(NodeKind kind) {
        _pendingLayoutKinds.Add(kind);
    }

    private void FlushPendingLayoutRecalculations() {
        if (_isRecalculating || _pendingLayoutKinds.Count == 0) {
            return;
        }

        var pendingKinds = _pendingLayoutKinds.ToArray();
        _pendingLayoutKinds.Clear();

        foreach (var kind in pendingKinds) {
            RecalculateAllLayouts(kind);
        }
    }

    private void ToggleEnabled(StatusTimerOverlayNode<StatusKey>? overlay, VerticalListNode group,
        NodeKind kind, bool isChecked) {
        if (overlay == null) {
            return;
        }

        overlay.IsVisible = isChecked;
        group.IsVisible = isChecked;
        group.Height = 0;
        QueueRecalculateAllLayouts(kind);
    }
}
