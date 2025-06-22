using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Nodes.TabBar;
using KamiToolKit.System;
using Lumina.Excel.Sheets;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace StatusTimers.Windows;

public class ConfigurationWindow : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private const float CheckBoxWidth = 300;
    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private readonly Dictionary<NodeKind, TextButtonNode> _configTabButtons = new();

    private readonly OverlayManager _overlayManager;
    private string _currentFilterInput = "10";
    private readonly EnemyMultiDoTOverlay _enemyMultiDoTOverlay;
    private readonly PlayerCombinedStatusesOverlay _playerCombinedStatusesOverlay;

    private static readonly Dictionary<GrowDirection, string> GrowDirectionMap = new()
    {
        { GrowDirection.DownRight, "Grow Down and Right" },
        { GrowDirection.DownLeft, "Grow Down and Left" },
        { GrowDirection.UpRight, "Grow Up and Right" },
        { GrowDirection.UpLeft, "Grow Up and Left" }
    };

    public ConfigurationWindow(OverlayManager overlayManager) {
        _overlayManager = overlayManager;
        _enemyMultiDoTOverlay = overlayManager.EnemyMultiDoTOverlayInstance;
        _playerCombinedStatusesOverlay = overlayManager.PlayerCombinedOverlayInstance;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        TabBarNode tabBar = new() {
            Position = ContentStartPosition,
            Size = ContentSize with { Y = 24 },
            Height = 24,
            IsVisible = true
        };

        const int buttonHeight = 28;
        const int buttonSpacing = 0;
        NodeKind[] nodeKinds = Enum.GetValues<NodeKind>();
        int buttonWidth = (int)MathF.Floor((ContentSize.X - buttonSpacing * (nodeKinds.Length - 1)) / nodeKinds.Length);

        int x = 0;

        /*
        tabBar.AddTab("Hello", () => Services.Logger.Info("Hello"));
        tabBar.AddTab("Okay", () => Services.Logger.Info("Hello"));
        */

        foreach ((NodeKind kind, int _) in nodeKinds.Select((kind, index) => (kind, index))) {
            tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));
            /*
            _configTabButtons[kind] = new TextButtonNode {
                X = x,
                Width = buttonWidth,
                Height = buttonHeight,
                IsVisible = true,
                Label = kind.ToString(),
                OnClick = () => OnTabButtonClick(kind)
            };
            NativeController.AttachNode(_configTabButtons[kind], tabBar);
            */

            _configScrollingAreas[kind] = new ScrollingAreaNode {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - tabBar.Height,
                ContentHeight = 2000.0f,
                IsVisible = false
            };
            NativeController.AttachNode(_configScrollingAreas[kind], this);

            _configLists[kind] = new VerticalListNode<NodeBase> {
                Height = 500,
                Width = buttonWidth,
                IsVisible = true,
                ItemVerticalSpacing = 3,

            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = 16,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(2),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Visual Settings"
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Enabled",
                IsChecked = GetOverlayByKind(kind).IsVisible,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).IsVisible = isChecked;
                },
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Locked",
                IsChecked = GetOverlayByKind(kind).IsLocked,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).IsLocked = isChecked;
                },
            });

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            var growDirectionNode = new TextDropDownNode {
                X = 324,
                IsVisible = true,
                Width = 230,
                Height = 24,
                OptionListHeight = 120,
                Options = GrowDirectionMap.Values.ToList(),
                OnOptionSelected = selectedDisplayName => {
                    GrowDirection selectedGrowDirection =
                        GrowDirectionMap.FirstOrDefault(pair => pair.Value == selectedDisplayName).Key;

                    if (selectedGrowDirection != default || selectedDisplayName == GrowDirectionMap[default]) {
                        GetOverlayByKind(kind).GrowDirection = selectedGrowDirection;
                    }
                },
                SelectedOption = GrowDirectionMap[GetOverlayByKind(kind).GrowDirection],
            };

            var containerNode = new ResNode {
                IsVisible = true,
                Width = 600,
                Height = 32,
            };

            _configLists[kind].Add(containerNode);

            NativeController.AttachNode(new TextNode {
                X = OptionOffset,
                Y = 0,
                IsVisible = true,
                Width = 300,
                Height = 20,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(8),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Grow direction",
            }, containerNode);

            NativeController.AttachNode(new TextNode {
                X = OptionOffset,
                Y = 20,
                IsVisible = true,
                Width = 300,
                Height = 20,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(8),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Statuses per row",
            }, containerNode);

            NativeController.AttachNode(growDirectionNode, containerNode);

            var slider = new SliderNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = 30,
                IsVisible = true,
                Min = 1,
                Max = 30,
                Value = GetOverlayByKind(kind).ItemsPerLine,
                Step = 1,
                OnValueChanged = (value) => GetOverlayByKind(kind).ItemsPerLine = value
            };

            _configLists[kind].Add(slider);

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Fill columns first",
                IsChecked = !GetOverlayByKind(kind).FillRowsFirst,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).FillRowsFirst = !isChecked;
                },
            });

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Show status name",
                IsChecked = GetOverlayByKind(kind).ShowStatusName,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).ShowStatusName = isChecked;
                },
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Show time remaining",
                IsChecked = GetOverlayByKind(kind).ShowStatusRemaining,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).ShowStatusRemaining = isChecked;
                },
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Show time remaining background",
                IsChecked = GetOverlayByKind(kind).ShowStatusRemainingBackground,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).ShowStatusRemainingBackground = isChecked;
                },
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Show progressbar",
                IsChecked = GetOverlayByKind(kind).ShowProgress,
                OnClick = isChecked => {
                    GetOverlayByKind(kind).ShowProgress = isChecked;
                },
            });

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Width = CheckBoxWidth,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Show enemy name",
                    IsChecked = GetOverlayByKind(kind).ShowActorName,
                    OnClick = isChecked => {
                        GetOverlayByKind(kind).ShowActorName = isChecked;
                    },
                });

                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Width = CheckBoxWidth,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Show enemy letter",
                    IsChecked = GetOverlayByKind(kind).ShowActorLetter,
                    OnClick = isChecked => {
                        GetOverlayByKind(kind).ShowActorLetter = isChecked;
                    },
                });
            }

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = 16,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(2),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Functional Settings"
            });

            if (kind == NodeKind.Combined) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Width = CheckBoxWidth,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Hide permanent statuses",
                    IsChecked = !GetOverlayByKind(kind).ShowPermaIcons,
                    OnClick = isChecked => {
                        GetOverlayByKind(kind).ShowPermaIcons = !isChecked;
                    },
                });
            }

            if (kind == NodeKind.Combined) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Width = CheckBoxWidth,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Allow dismissing status by right-clicking the status icon.",
                    IsChecked = GetOverlayByKind(kind).AllowDismissStatus,
                    OnClick = isChecked => {
                        GetOverlayByKind(kind).AllowDismissStatus = isChecked;
                    },
                });
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Width = CheckBoxWidth,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Allow targeting the enemy by clicking the status icon.",
                    IsChecked = GetOverlayByKind(kind).AllowTargetActor,
                    OnClick = isChecked => {
                        GetOverlayByKind(kind).AllowTargetActor = isChecked;
                    },
                });
            }

            TextInputNode filterListInput = new() {
                X = OptionOffset - 4,
                Width = CheckBoxWidth,
                Height = buttonHeight,
                Size = new Vector2(300, 28),
                IsVisible = true,
                OnInputReceived = input => {
                    _currentFilterInput = input.TextValue.ToString();
                }
            };
            _configLists[kind].Add(filterListInput);

            LuminaDropDownNode<Status> filterDropdown = new() {
                X = OptionOffset,
                Width = CheckBoxWidth,
                Height = buttonHeight,
                IsVisible = true,
                OptionListHeight = 125.0f,

                //FilterFunction = status => status.Icon != 0 && !status.Name.ExtractText().IsNullOrEmpty() &&
                //                           (status.Name.ContainsText(Encoding.UTF8.GetBytes(_currentFilterInput)) || status.RowId.ToString().Contains(_currentFilterInput)),
                FilterFunction = status => status.RowId.ToString().Contains(_currentFilterInput),
                LabelFunction = status => $"{status.RowId} {status.Name.ExtractText()}"
            };
            _configLists[kind].Add(filterDropdown);

            //NativeController.AttachNode(filterDropdown, _configLists[kind]);

            x += buttonWidth + buttonSpacing;
        }

        NativeController.AttachNode(tabBar, this);
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode node) in _configScrollingAreas) {
            node.IsVisible = k == kind;
        }
    }

    private StatusTimerOverlay<StatusKey> GetOverlayByKind(NodeKind kind) {
        switch (kind) {
            case NodeKind.Combined:
                return _playerCombinedStatusesOverlay;
            case NodeKind.MultiDoT:
                return _enemyMultiDoTOverlay;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind,
                    "The provided NodeKind is not supported by GetOverlayByKind.");
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
    }
}
