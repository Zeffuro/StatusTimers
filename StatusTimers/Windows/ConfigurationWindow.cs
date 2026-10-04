using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float CheckBoxHeight = 16;

    private readonly Dictionary<NodeKind, ScrollingNode<VerticalListNode>?> _configScrollingAreas = new();

    private readonly Dictionary<NodeKind, FilterSectionNode> _filterSectionNodes = new();
    private TabBarNode? _tabBar;
    private ScrollingNode<VerticalListNode>? _profilesArea;
    private ScrollingNode<VerticalListNode>? _assignmentsArea;
    private bool _assignmentsSelected;
    private bool _rebuildRequested;
    private bool _rebuildOverlaysRequested;
    private NodeKind? _selectedKind;

    public void RequestRebuild(bool overlaysChanged = false) {
        _rebuildRequested = true;
        _rebuildOverlaysRequested |= overlaysChanged;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        _configScrollingAreas.Clear();
        _filterSectionNodes.Clear();
        _tabBar = null;
        _profilesArea = null;
        _assignmentsArea = null;
        _rebuildRequested = false;
        _rebuildOverlaysRequested = false;

        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        CollapseAllDropdowns();
        _selectedKind = kind;
        _assignmentsSelected = false;
        if (_assignmentsArea != null) {
            _assignmentsArea.IsVisible = false;
        }

        if (!_configScrollingAreas.ContainsKey(kind)) {
            SetupOverlayOptions(kind);
        }

        if (_profilesArea != null) {
            _profilesArea.IsVisible = false;
        }

        foreach ((NodeKind k, ScrollingNode<VerticalListNode>? node) in
                 _configScrollingAreas) {
            node?.IsVisible = k == kind;
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (_rebuildRequested) {
            _rebuildRequested = false;
            CollapseAllDropdowns();
            _profilesArea?.Dispose();
            SetupProfileOptions();
            var assignmentScroll = _assignmentsArea?.ScrollBarNode.ScrollPosition ?? 0;
            _assignmentsArea?.Dispose();
            _assignmentsArea = null;
            if (_assignmentsSelected) {
                SetupAssignmentOptions();
                _assignmentsArea!.ScrollBarNode.ScrollPosition = Math.Min(assignmentScroll,
                    _assignmentsArea.ScrollBarNode.ScrollMaxPosition);
            }
            if (_rebuildOverlaysRequested) {
                _rebuildOverlaysRequested = false;
                foreach (var area in _configScrollingAreas.Values) {
                    area?.Dispose();
                }

                _configScrollingAreas.Clear();
                _filterSectionNodes.Clear();
                if (_selectedKind is { } selected) {
                    OnTabButtonClick(selected);
                }
            }
        }
        if (_selectedKind is { } kind && _filterSectionNodes.TryGetValue(kind, out var filterNode)) {
            filterNode.OnUpdate();
        }
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        CollapseAllDropdowns();
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

        _tabBar.AddTab("Profiles", () => {
            CollapseAllDropdowns();
            _selectedKind = null;
            _assignmentsSelected = false;
            if (_assignmentsArea != null) {
                _assignmentsArea.IsVisible = false;
            }

            foreach (var area in _configScrollingAreas.Values) {
                if (area != null) {
                    area.IsVisible = false;
                }
            }

            if (_profilesArea != null) {
                _profilesArea.IsVisible = true;
            }
        });
        _tabBar.AddTab("Assignments", OnAssignmentsTabClick);
        foreach (var kind in Enum.GetValues<NodeKind>()) {
            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind),
                isEnabled: GetOverlayByKind(kind)?.OverlayConfig != null);
        }
        SetupProfileOptions();
        if (_assignmentsSelected) {
            _tabBar.SelectTab("Assignments");
            OnAssignmentsTabClick();
        }
        else if (_selectedKind is { } selected) {
            _tabBar.SelectTab(selected.ToString());
            OnTabButtonClick(selected);
        }
    }

    private void SetupProfileOptions() {
        _profilesArea = new ScrollingNode<VerticalListNode> {
            Position = ContentStartPosition + new Vector2(0, 24),
            Size = ContentSize with { Y = ContentSize.Y - 24 },
            ScrollSpeed = 50,
            IsVisible = _selectedKind == null && !_assignmentsSelected,
            ContentNode = { FitContents = true, ItemSpacing = 6 }
        };
        _profilesArea.AttachNode(this);
        _profilesArea.ContentNode.Width = _profilesArea.Width;
        _profilesArea.ContentNode.IsVisible = true;
        _profilesArea.ContentNode.AddNode(new ProfileSectionNode(overlayManager));
        _profilesArea.RecalculateSizes();
    }

    private void OnAssignmentsTabClick() {
        CollapseAllDropdowns();
        _selectedKind = null;
        _assignmentsSelected = true;
        if (_profilesArea != null) {
            _profilesArea.IsVisible = false;
        }

        foreach (var area in _configScrollingAreas.Values) {
            if (area != null) {
                area.IsVisible = false;
            }
        }

        if (_assignmentsArea == null) {
            SetupAssignmentOptions();
        }

        _assignmentsArea!.IsVisible = true;
    }

    private void SetupAssignmentOptions() {
        _assignmentsArea = new ScrollingNode<VerticalListNode> {
            Position = ContentStartPosition + new Vector2(0, 24),
            Size = ContentSize with { Y = ContentSize.Y - 24 },
            ScrollSpeed = 50,
            IsVisible = _assignmentsSelected,
            ContentNode = { FitContents = true, ItemSpacing = 6 }
        };
        _assignmentsArea.AttachNode(this);
        _assignmentsArea.ContentNode.Width = _assignmentsArea.Width;
        _assignmentsArea.ContentNode.IsVisible = true;
        _assignmentsArea.ContentNode.AddNode(new ProfileAssignmentsNode(overlayManager, () => {
            _assignmentsArea.ContentNode.RecalculateLayout();
            _assignmentsArea.RecalculateSizes();
        }));
        _assignmentsArea.RecalculateSizes();
    }

    private void SetupOverlayOptions(NodeKind kind) {
        var overlay = GetOverlayByKind(kind);
        if (overlay?.OverlayConfig == null) {
            return;
        }

        overlay.IsVisible = overlay.OverlayConfig.Enabled;

        var configScrollingArea = new ScrollingNode<VerticalListNode> {
            Position = ContentStartPosition + new Vector2(0, 24),
            ContentNode = {
                FitContents = true,
                ItemSpacing = 3
            },
            Size = ContentSize with { Y = ContentSize.Y - 24 },
            ScrollSpeed = 50,
            IsVisible = false
        };
        configScrollingArea.AttachNode(this);
        _configScrollingAreas[kind] = configScrollingArea;

        var list = configScrollingArea.ContentNode;
        list.IsVisible = true;
        list.Width = configScrollingArea.Width;
        list.Height = 0;

        var listNodes = new List<NodeBase>();

        var mainSettingsGroup = new VerticalListNode {
            IsVisible = overlay.OverlayConfig.Enabled,
            Height = 0,
            Width = list.Width,
            FitContents = true,
            ItemSpacing = 3
        };
        var mainSettingsNodes = new List<NodeBase>();

        listNodes.Add(
            new ImportExportResetNode(
                overlayManager, kind
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
                Width = 600,
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
                onToggled: () => RecalculateAllLayouts(kind)
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
                onToggled: () => RecalculateAllLayouts(kind)
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
                onToggled: () => RecalculateAllLayouts(kind)
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
                    RecalculateAllLayouts(kind);
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
                onToggled: () => RecalculateAllLayouts(kind)
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
                    onToggled: () => RecalculateAllLayouts(kind)
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
            });

        mainSettingsNodes.Add(new SortingSectionNode(() => overlay.OverlayConfig, kind) {
            IsVisible = true,
            Width = 600,
        });

        _filterSectionNodes[kind] = new FilterSectionNode(
            () => overlay.OverlayConfig,
            onChanged: () => {
                overlay.OverlayConfig.Notify(nameof(overlay.OverlayConfig.FilterList));
                RecalculateAllLayouts(kind);

                _configScrollingAreas[kind]?.ScrollToEnd();
            }
        ) {
            IsVisible = true,
            Width = 600,
        };
        mainSettingsNodes.Add(_filterSectionNodes[kind]);

        mainSettingsGroup.AddNode(mainSettingsNodes);
        listNodes.Add(mainSettingsGroup);
        list.AddNode(listNodes);

        RecalculateAllLayouts(kind);
    }

    private static void CollapseDropdowns(NodeBase node) {
        if (node is StringDropDownNode dropdown) {
            dropdown.Collapse(false);
        }

        if (node is ILayoutListNode list) {
            foreach (var child in list.Nodes) {
                CollapseDropdowns(child);
            }
        }
    }

    private void CollapseAllDropdowns() {
        if (_profilesArea != null) {
            CollapseDropdowns(_profilesArea.ContentNode);
        }

        if (_assignmentsArea != null) {
            CollapseDropdowns(_assignmentsArea.ContentNode);
        }

        foreach (var area in _configScrollingAreas.Values) {
            if (area != null) {
                CollapseDropdowns(area.ContentNode);
            }
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
        var scrollingArea = _configScrollingAreas[kind];
        if (scrollingArea == null) {
            return;
        }

        scrollingArea.ContentNode.RecalculateLayout();
        scrollingArea.RecalculateSizes();
    }

    private void ToggleEnabled(StatusTimerOverlayNode<StatusKey>? overlay, VerticalListNode group,
        NodeKind kind, bool isChecked) {
        if (overlay == null) {
            return;
        }

        overlay.IsVisible = isChecked;
        group.IsVisible = isChecked;
        RecalculateAllLayouts(kind);
    }
}
