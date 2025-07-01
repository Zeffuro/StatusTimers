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
    //private ModalNode _modal;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        //_modal = new ModalNode();
        //NativeController.AttachNode(_modal.RootNode, this);
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
            StatusTimerOverlayConfig? currentOverlayConfig = GetOverlayByKind(kind)?.OverlayConfig;
            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));

            if (overlay == null || currentOverlayConfig == null) {
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
                ItemVerticalSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            var mainSettingsGroup = new VerticalListNode<NodeBase> {
                IsVisible = overlay.IsVisible,
                FitContents = true,
                ItemVerticalSpacing = 3
            };

            _configLists[kind].AddNode(
                ImportExportResetUIFactory.Create(currentOverlayConfig, kind,
                    onConfigChanged: () => { },
                    closeWindow: Close)
            );

            var enabledCheckbox = ConfigurationUIFactory.CreateCheckboxOption(
                "Enabled",
                () => overlay.IsVisible,
                isChecked => {
                    ToggleEnabled(overlay, mainSettingsGroup, kind, isChecked);
                }
            );

            _configLists[kind].AddNode(enabledCheckbox);

            mainSettingsGroup.AddDummy(new ResNode(), CheckBoxHeight);

            mainSettingsGroup.AddNode(
                ColorPickerTemp.CreateColorPickerSection(
                    currentOverlayConfig,
                    overlayManager,
                    onChanged: () => { }
                )
            );

            // Visual Settings
            mainSettingsGroup.AddNode(
                VisualSettingsUIFactory.Create(
                    overlay,
                    currentOverlayConfig,
                    onChanged: () => { },
                    optionOffset: OptionOffset,
                    checkBoxHeight: CheckBoxHeight
                )
            );

            // Icon Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "icon",
                    currentOverlayConfig.StatusNodeLayout.IconAnchor,
                    getEnabled: () => currentOverlayConfig.ShowIcon,
                    setEnabled: v => currentOverlayConfig.ShowIcon = v,
                    onChanged: () => {
                        currentOverlayConfig.Notify(
                            nameof(currentOverlayConfig.StatusNodeLayout),
                            needsRebuild: true
                        );
                    },
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Name Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "status name",
                    currentOverlayConfig.StatusNodeLayout.NameAnchor,
                    getEnabled: () => currentOverlayConfig.ShowStatusName,
                    setEnabled: v => currentOverlayConfig.ShowStatusName = v,
                    getStyle: () => currentOverlayConfig.StatusNameTextStyle,
                    setStyle: s => currentOverlayConfig.StatusNameTextStyle = s,
                    onChanged: () => {
                        currentOverlayConfig.Notify(
                            nameof(currentOverlayConfig.StatusNodeLayout),
                            needsRebuild: true
                        );
                    },
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Status Time Remaining Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "time remaining",
                    currentOverlayConfig.StatusNodeLayout.TimerAnchor,
                    getEnabled: () => currentOverlayConfig.ShowStatusRemaining,
                    setEnabled: v => currentOverlayConfig.ShowStatusRemaining = v,
                    getStyle: () => currentOverlayConfig.StatusRemainingTextStyle,
                    setStyle: s => currentOverlayConfig.StatusRemainingTextStyle = s,
                    onChanged: () => {
                        currentOverlayConfig.Notify(
                            nameof(currentOverlayConfig.StatusNodeLayout),
                            needsRebuild: true
                        );
                    },
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Progress Bar Settings
            mainSettingsGroup.AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "progressbar",
                    currentOverlayConfig.StatusNodeLayout.ProgressAnchor,
                    getEnabled: () => currentOverlayConfig.ShowProgress,
                    setEnabled: v => currentOverlayConfig.ShowProgress = v,
                    onChanged: () => {
                        currentOverlayConfig.Notify(
                            nameof(currentOverlayConfig.StatusNodeLayout),
                            needsRebuild: true
                        );
                    },
                    onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                )
            );

            // Actor Name Settings
            if (kind == NodeKind.MultiDoT) {
                mainSettingsGroup.AddNode(
                    NodeLayoutUIFactory.CreateNodeLayoutSection(
                        "enemy name",
                        currentOverlayConfig.StatusNodeLayout.ActorNameAnchor,
                        getEnabled: () => currentOverlayConfig.ShowActorName,
                        setEnabled: v => currentOverlayConfig.ShowActorName = v,
                        getStyle: () => currentOverlayConfig.ActorNameTextStyle,
                        setStyle: s => currentOverlayConfig.ActorNameTextStyle = s,
                        onChanged: () => {
                            currentOverlayConfig.Notify(
                                nameof(currentOverlayConfig.StatusNodeLayout),
                                needsRebuild: true
                            );
                        },
                        onToggled: () => RecalculateAllLayouts(mainSettingsGroup, kind)
                    )
                );
                mainSettingsGroup.AddNode(ConfigurationUIFactory.CreateCheckboxOption("Show enemy letter",
                    () => currentOverlayConfig.ShowActorLetter,
                    isChecked => currentOverlayConfig.ShowActorLetter = isChecked));
            }

            mainSettingsGroup.AddDummy(new ResNode(), CheckBoxHeight);

            // Functional Settings
            mainSettingsGroup.AddNode(
                FunctionalSettingsUIFactory.Create(
                    currentOverlayConfig,
                    kind,
                    onChanged: () => { },
                    checkBoxHeight: CheckBoxHeight
                )
            );

            // Sorting Priority Settings
            mainSettingsGroup.AddNode(
                SortUIFactory.CreateSortPrioritySection(currentOverlayConfig, kind)
            );

            // Filtering Settings
            void UpdateFilterSection() {
                var oldNode = _filterSectionNodes.ContainsKey(kind) ? _filterSectionNodes[kind] : null;
                var newNode = FilterUIFactory.CreateFilterSection(currentOverlayConfig,
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
