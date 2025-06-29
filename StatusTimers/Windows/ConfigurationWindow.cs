using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Nodes.TabBar;
using KamiToolKit.System;
using Lumina.Excel.Sheets;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Factories;
using StatusTimers.Helpers;
using StatusTimers.Interfaces;
using StatusTimers.Layout;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private const float CheckBoxWidth = 300;
    private const float ButtonHeight = 30;

    private static readonly Dictionary<GrowDirection, string> GrowDirectionMap = new() {
        { GrowDirection.DownRight, "Down and Right" },
        { GrowDirection.DownLeft, "Down and Left" },
        { GrowDirection.UpRight, "Up and Right" },
        { GrowDirection.UpLeft, "Up and Left" }
    };

    private static readonly Dictionary<FontType, string> FontMap = new() {
        { FontType.Axis, "Axis" },
        { FontType.Miedinger, "Miedinger" },
        { FontType.TrumpGothic, "Trump Gothic" },
        { FontType.Jupiter, "Jupiter" },
        { FontType.JupiterLarge, "Jupiter Large" }
    };

    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _filterSectionNodes = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private TabBarNode _tabBar;
    //private ModalNode _modal;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        //_modal = new ModalNode();
        //NativeController.AttachNode(_modal.RootNode, this);
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
                overlay.IsPreviewEnabled = false;
                overlay.IsLocked = true;
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
            StatusTimerOverlayConfig currentOverlayConfig = GetOverlayByKind(kind).OverlayConfig;
            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));

            _configScrollingAreas[kind] = new ScrollingAreaNode {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + _tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - _tabBar.Height,
                ContentHeight = 2000.0f,
                IsVisible = false
            };
            NativeController.AttachNode(_configScrollingAreas[kind], this);

            _configLists[kind] = new VerticalListNode<NodeBase> {
                Height = 500,
                Width = _configScrollingAreas[kind].ContentNode.Width,
                IsVisible = true,
                ItemVerticalSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                new CircleButtonNode {
                    Height = ButtonHeight,
                    Width = ButtonHeight,
                    IsVisible = true,
                    Tooltip = "Export Configuration",
                    Icon = ButtonIcon.Document,
                    AddColor = new Vector3(150, 0, 150),
                    OnClick = () => {/*
                        _modal.Show("Yessss", (chosen) => {
                            GlobalServices.Logger.Info($"Chosen {chosen}");
                        });
                        GlobalServices.Logger.Info("Export");*/
                    }
                },
                new CircleButtonNode {
                    X = 52,
                    Height = ButtonHeight,
                    Width = ButtonHeight,
                    IsVisible = true,
                    Tooltip = "Import Configuration",
                    Icon = ButtonIcon.Document,
                    AddColor = new Vector3(0, 150, 150),
                    OnClick = () => {
                        GlobalServices.Logger.Info("Import");
                    }
                },
                ButtonHeight
                ));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Visual Settings"
            });

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                ConfigurationUIFactory.CreateCheckboxOption("Enabled",
                    () => overlay.IsVisible,
                    isChecked => overlay.IsVisible = isChecked),
                ConfigurationUIFactory.CreateCheckboxOption("Preview Mode",
                    () => overlay.IsPreviewEnabled,
                    isChecked => overlay.IsPreviewEnabled = isChecked),
                CheckBoxHeight)
            );
            _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption("Locked",
                () => overlay.IsLocked,
                isChecked => overlay.IsLocked = isChecked));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(new TextNode {
                        IsVisible = true,
                        X = OptionOffset,
                        Width = 120,
                        Height = TextStyles.OptionLabel.Height,
                        FontSize = TextStyles.Defaults.FontSize,
                        TextColor = TextStyles.OptionLabel.TextColor,
                        TextOutlineColor = TextStyles.Defaults.OutlineColor,
                        TextFlags = TextStyles.Defaults.Flags,
                        Text = "Scale"
                    },
                    ConfigurationUIFactory.CreateLabeledNumericOption("Horizontal Padding",
                        () => currentOverlayConfig.StatusHorizontalPadding,
                        value => currentOverlayConfig.StatusHorizontalPadding = value),
                    CheckBoxHeight
                )
            );

            _configLists[kind].AddNode();

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                ConfigurationUIFactory.CreateSliderOption(
                    5,
                    200,
                    5,
                    () => currentOverlayConfig.ScaleInt,
                    value => currentOverlayConfig.ScaleInt = value
                ),
                ConfigurationUIFactory.CreateLabeledNumericOption("Vertical Padding",
                    () => currentOverlayConfig.StatusVerticalPadding,
                    value => currentOverlayConfig.StatusVerticalPadding = value),
                30)
            );

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            TextNode statusPerLineNode = new() {
                X = OptionOffset,
                IsVisible = true,
                Width = 300,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = $"Statuses per {(currentOverlayConfig.FillRowsFirst ? "row" : "column")}"
            };

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                    statusPerLineNode,
                    new TextNode {
                        X = OptionOffset,
                        IsVisible = true,
                        Width = 300,
                        Height = TextStyles.OptionLabel.Height,
                        FontSize = TextStyles.Defaults.FontSize,
                        TextColor = TextStyles.OptionLabel.TextColor,
                        TextOutlineColor = TextStyles.Defaults.OutlineColor,
                        TextFlags = TextStyles.Defaults.Flags,
                        Text = "Max statuses displayed"
                    },
                    TextStyles.OptionLabel.Height
                )
            );

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                    ConfigurationUIFactory.CreateSliderOption(
                        1,
                        30,
                        1,
                        () => currentOverlayConfig.ItemsPerLine,
                        value => currentOverlayConfig.ItemsPerLine = value
                    ),
                    ConfigurationUIFactory.CreateSliderOption(
                        1,
                        30,
                        1,
                        () => currentOverlayConfig.MaxStatuses,
                        value => currentOverlayConfig.MaxStatuses = value
                    ),
                    30
                )
            );

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateTwoOptionsRow(
                new ResNode(),
                ConfigurationUIFactory.CreateLabeledDropdown(
                    "Grow direction",
                    () => currentOverlayConfig.GrowDirection,
                    value => currentOverlayConfig.GrowDirection = value,
                    GrowDirectionMap
                ),
                CheckBoxHeight
            ));

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption("Fill columns first",
                () => !currentOverlayConfig.FillRowsFirst,
                isChecked => {
                    currentOverlayConfig.FillRowsFirst = !isChecked;
                    statusPerLineNode.Text = $"Statuses per {(currentOverlayConfig.FillRowsFirst ? "row" : "column")}";
                }));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "icon",
                    currentOverlayConfig.StatusNodeLayout.IconAnchor,
                    getEnabled: () => currentOverlayConfig.ShowIcon,
                    setEnabled: v => currentOverlayConfig.ShowIcon = v,
                    onChanged: () => { }
                )
            );
            _configLists[kind].AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "status name",
                    currentOverlayConfig.StatusNodeLayout.NameAnchor,
                    getEnabled: () => currentOverlayConfig.ShowStatusName,
                    setEnabled: v => currentOverlayConfig.ShowStatusName = v,
                    getStyle: () => currentOverlayConfig.StatusNameTextStyle,
                    setStyle: s => currentOverlayConfig.StatusNameTextStyle = s,
                    onChanged: () => { }
                )
            );
            _configLists[kind].AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "time remaining",
                    currentOverlayConfig.StatusNodeLayout.TimerAnchor,
                    getEnabled: () => currentOverlayConfig.ShowStatusRemaining,
                    setEnabled: v => currentOverlayConfig.ShowStatusRemaining = v,
                    getStyle: () => currentOverlayConfig.StatusRemainingTextStyle,
                    setStyle: s => currentOverlayConfig.StatusRemainingTextStyle = s,
                    onChanged: () => { }
                )
            );
            _configLists[kind].AddNode(
                NodeLayoutUIFactory.CreateNodeLayoutSection(
                    "progressbar",
                    currentOverlayConfig.StatusNodeLayout.ProgressAnchor,
                    getEnabled: () => currentOverlayConfig.ShowProgress,
                    setEnabled: v => currentOverlayConfig.ShowProgress = v,
                    onChanged: () => { }
                )
            );

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddNode(
                    NodeLayoutUIFactory.CreateNodeLayoutSection(
                        "enemy name",
                        currentOverlayConfig.StatusNodeLayout.ActorNameAnchor,
                        getEnabled: () => currentOverlayConfig.ShowActorName,
                        setEnabled: v => currentOverlayConfig.ShowStatusRemaining = v,
                        getStyle: () => currentOverlayConfig.StatusRemainingTextStyle,
                        setStyle: s => currentOverlayConfig.StatusRemainingTextStyle = s,
                        onChanged: () => { }
                    )
                );
                _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption("Show enemy letter",
                    () => currentOverlayConfig.ShowActorLetter,
                    isChecked => currentOverlayConfig.ShowActorLetter = isChecked));
            }

            _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption("Animations Enabled",
                () => currentOverlayConfig.AnimationsEnabled,
                isChecked => currentOverlayConfig.AnimationsEnabled = isChecked));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Functional Settings"
            });

            if (kind == NodeKind.Combined) {
                _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption("Hide permanent statuses",
                    () => !currentOverlayConfig.ShowPermaIcons,
                    isChecked => currentOverlayConfig.ShowPermaIcons = !isChecked));

                _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                    "Allow dismissing status by right-clicking the status icon.",
                    () => currentOverlayConfig.AllowDismissStatus,
                    isChecked => currentOverlayConfig.AllowDismissStatus = isChecked));
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddNode(ConfigurationUIFactory.CreateCheckboxOption(
                    "Allow targeting the enemy by clicking the status icon.",
                    () => currentOverlayConfig.AllowTargetActor,
                    isChecked => currentOverlayConfig.AllowTargetActor = isChecked));
            }

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            // Sorting Priority
            _configLists[kind].AddNode(
                SortUIFactory.CreateSortPrioritySection(currentOverlayConfig, kind)
            );

            // Filtering
            void UpdateFilterSection() {
                var oldNode = _filterSectionNodes.ContainsKey(kind) ? _filterSectionNodes[kind] : null;
                var newNode = FilterUIFactory.CreateFilterSection(currentOverlayConfig, UpdateFilterSection);
                _filterSectionNodes[kind] = newNode;
                if (oldNode != null) {
                    _configLists[kind].RemoveNode(oldNode);
                }

                _configLists[kind].AddNode(newNode);
            }
            UpdateFilterSection();

        }

        NativeController.AttachNode(_tabBar, this);
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    #region Helper Methods

    private StatusTimerOverlay<StatusKey> GetOverlayByKind(NodeKind kind) {
        return (kind switch {
            NodeKind.Combined => overlayManager.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported NodeKind")
        })!;
    }

    #endregion
}
