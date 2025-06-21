using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

public class ConfigurationWindow : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private readonly Dictionary<NodeKind, TextButtonNode> _configTabButtons = new();
    private string _currentFilterInput = "10";

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SimpleComponentNode tabBar = new() {
            Position = ContentStartPosition,
            Width = ContentSize.X,
            Height = 28,
            IsVisible = true
        };
        NativeController.AttachNode(tabBar, this);

        const int buttonHeight = 28;
        const int buttonSpacing = 0;
        NodeKind[] nodeKinds = Enum.GetValues<NodeKind>();
        int buttonWidth = (int)MathF.Floor((ContentSize.X - buttonSpacing * (nodeKinds.Length - 1)) / nodeKinds.Length);

        int x = 0;
        foreach ((NodeKind kind, int _) in nodeKinds.Select((kind, index) => (kind, index))) {
            _configTabButtons[kind] = new TextButtonNode {
                X = x,
                Width = buttonWidth,
                Height = buttonHeight,
                IsVisible = true,
                Label = kind.ToString(),
                OnClick = () => OnTabButtonClick(kind)
            };
            NativeController.AttachNode(_configTabButtons[kind], tabBar);

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
                ItemVerticalSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            _configLists[kind].Add(new ResNode
            {
                Height = CheckBoxHeight,
            });

            _configLists[kind].Add(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = 16,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(2),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Visual"
            });

            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Height = CheckBoxHeight,
                IsVisible = true,
                LabelText = "Locked"
            });

            _configLists[kind].Add(new ResNode
            {
                Height = CheckBoxHeight,
            });

            _configLists[kind].Add(new SliderNode {
                X = OptionOffset,
                Height = 30,
                IsVisible = true,
                Min = 0,
                Max = 4,
                Value = 1,
                Step = 1,
            });
            _configLists[kind].Add(new CheckboxNode {
                X = OptionOffset,
                Height = CheckBoxHeight,
                Width = 200,
                IsVisible = true,
                LabelText = "Show Progressbar"
            });
            if (kind == NodeKind.Combined) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Hide permanent statuses"
                });
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Show Enemy Name"
                });
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Show Enemy Letter"
                });
            }

            _configLists[kind].Add(new ResNode
            {
                Height = CheckBoxHeight,
            });

            _configLists[kind].Add(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = 16,
                FontSize = 14,
                TextColor = ColorHelper.GetColor(2),
                TextOutlineColor = ColorHelper.GetColor(0),
                TextFlags = TextFlags.Edge,
                Text = "Functional"
            });

            if (kind == NodeKind.Combined) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Allow dismissing status by right-clicking the status icon."
                });
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].Add(new CheckboxNode {
                    X = OptionOffset,
                    Height = CheckBoxHeight,
                    IsVisible = true,
                    LabelText = "Allow targeting the enemy by clicking the status icon."
                });
            }

            TextInputNode filterListInput = new() {
                X = OptionOffset,
                Height = buttonHeight,
                Size = new Vector2(300, 28),
                IsVisible = true,
                OnInputReceived = input => {
                    _currentFilterInput = input.TextValue.ToString();
                    //filterDropdown.FilterFunction
                }
            };
            _configLists[kind].Add(filterListInput);

            LuminaDropDownNode<Status> filterDropdown = new() {
                X = OptionOffset,
                Height = buttonHeight,
                Size = new Vector2(300, 28),
                IsVisible = true,
                OptionListHeight = 125.0f,

                /*FilterFunction = status => status.Icon != 0 && !status.Name.ExtractText().IsNullOrEmpty() &&
                                           (status.Name.ContainsText(Encoding.UTF8.GetBytes(_currentFilterInput)) || status.RowId.ToString().Contains(_currentFilterInput)),*/
                FilterFunction = status => status.RowId.ToString().Contains(_currentFilterInput),
                LabelFunction = status => $"{status.RowId} {status.Name.ExtractText()}"
            };
            _configLists[kind].Add(filterDropdown);
            //NativeController.AttachNode(filterDropdown, _configLists[kind]);

            x += buttonWidth + buttonSpacing;
        }

        _configScrollingAreas.First().Value.IsVisible = true;
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, TextButtonNode button) in _configTabButtons) {
            button.IsChecked = k == kind;
        }

        foreach ((NodeKind k, ScrollingAreaNode node) in _configScrollingAreas) {
            node.IsVisible = k == kind;
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
    }
}
