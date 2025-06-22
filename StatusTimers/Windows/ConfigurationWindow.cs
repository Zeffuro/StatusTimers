using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Nodes.TabBar;
using KamiToolKit.System;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private const float CheckBoxWidth = 300;

    private static readonly Dictionary<GrowDirection, string> GrowDirectionMap = new() {
        { GrowDirection.DownRight, "Grow Down and Right" },
        { GrowDirection.DownLeft, "Grow Down and Left" },
        { GrowDirection.UpRight, "Grow Up and Right" },
        { GrowDirection.UpLeft, "Grow Up and Left" }
    };

    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        TabBarNode tabBar = new() {
            Position = ContentStartPosition,
            Size = ContentSize with { Y = 24 },
            Height = 24,
            IsVisible = true
        };
        const int buttonSpacing = 0;
        NodeKind[] nodeKinds = Enum.GetValues<NodeKind>();

        foreach ((NodeKind kind, int _) in nodeKinds.Select((kind, index) => (kind, index))) {
            IOverlayConfiguration currentOverlayConfig = GetOverlayByKind(kind);
            tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));

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
                Width = _configScrollingAreas[kind].ContentNode.Width,
                IsVisible = true,
                ItemVerticalSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Visual Settings"
            });

            _configLists[kind].Add(CreateCheckboxOption("Enabled",
                () => currentOverlayConfig.IsVisible,
                isChecked => currentOverlayConfig.IsVisible = isChecked));

            _configLists[kind].Add(CreateCheckboxOption("Locked",
                () => currentOverlayConfig.IsLocked,
                isChecked => currentOverlayConfig.IsLocked = isChecked));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(CreateLabeledDropdown(
                "Grow direction",
                () => currentOverlayConfig.GrowDirection,
                value => currentOverlayConfig.GrowDirection = value,
                GrowDirectionMap
            ));

            _configLists[kind].Add(new TextNode {
                X = OptionOffset,
                IsVisible = true,
                Width = 300,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Items per line"
            });

            _configLists[kind].Add(CreateSliderOption(
                1,
                30,
                1,
                () => currentOverlayConfig.ItemsPerLine,
                value => currentOverlayConfig.ItemsPerLine = value
            ));

            _configLists[kind].Add(CreateCheckboxOption("Fill columns first",
                () => !currentOverlayConfig.FillRowsFirst,
                isChecked => currentOverlayConfig.FillRowsFirst = !isChecked));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(CreateCheckboxOption("Show status name",
                () => currentOverlayConfig.ShowStatusName,
                isChecked => currentOverlayConfig.ShowStatusName = isChecked));

            _configLists[kind].Add(CreateCheckboxOption("Show time remaining",
                () => currentOverlayConfig.ShowStatusRemaining,
                isChecked => currentOverlayConfig.ShowStatusRemaining = isChecked));

            _configLists[kind].Add(CreateCheckboxOption("Show time remaining background",
                () => currentOverlayConfig.ShowStatusRemainingBackground,
                isChecked => currentOverlayConfig.ShowStatusRemainingBackground = isChecked));

            _configLists[kind].Add(CreateCheckboxOption("Show progressbar",
                () => currentOverlayConfig.ShowProgress,
                isChecked => currentOverlayConfig.ShowProgress = isChecked));

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

                _configLists[kind].Add(CreateCheckboxOption("Show enemy name",
                    () => currentOverlayConfig.ShowActorName,
                    isChecked => currentOverlayConfig.ShowActorName = isChecked));

                _configLists[kind].Add(CreateCheckboxOption("Show enemy letter",
                    () => currentOverlayConfig.ShowActorLetter,
                    isChecked => currentOverlayConfig.ShowActorLetter = isChecked));
            }

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].Add(new TextNode {
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
                _configLists[kind].Add(CreateCheckboxOption("Hide permanent statuses",
                    () => !currentOverlayConfig.ShowPermaIcons,
                    isChecked => currentOverlayConfig.ShowPermaIcons = !isChecked));

                _configLists[kind].Add(CreateCheckboxOption(
                    "Allow dismissing status by right-clicking the status icon.",
                    () => currentOverlayConfig.AllowDismissStatus,
                    isChecked => currentOverlayConfig.AllowDismissStatus = isChecked));
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].Add(CreateCheckboxOption("Allow targeting the enemy by clicking the status icon.",
                    () => currentOverlayConfig.AllowTargetActor,
                    isChecked => currentOverlayConfig.AllowTargetActor = isChecked));
            }
        }

        NativeController.AttachNode(tabBar, this);
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode node) in _configScrollingAreas) {
            node.IsVisible = k == kind;
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
    }

    #region Helper Methods

    private CheckboxNode CreateCheckboxOption(string label, Func<bool> getter, Action<bool> setter) {
        CheckboxNode checkboxNode = new() {
            X = OptionOffset,
            Width = CheckBoxWidth,
            Height = CheckBoxHeight,
            IsVisible = true,
            LabelText = label,
            IsChecked = getter(),
            OnClick = setter
        };
        return checkboxNode;
    }

    private HorizontalFlexNode<NodeBase> CreateLabeledDropdown<TEnum>(
        string labelText,
        Func<TEnum> getter,
        Action<TEnum> setter,
        IReadOnlyDictionary<TEnum, string> enumToDisplayNameMap
    )
        where TEnum : Enum {
        HorizontalFlexNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 600,
            Height = 24,
            FitWidth = false,
            FitHeight = true,
            FitPadding = 4
        };

        TextNode labelNode = new() {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 260,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = labelText
        };
        flexNode.Add(labelNode);

        TextDropDownNode dropdownNode = new() {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 280,
            Height = 24,
            OptionListHeight = 120,
            Options = enumToDisplayNameMap.Values.ToList(),
            OnOptionSelected = selectedDisplayName => {
                TEnum selectedEnum = enumToDisplayNameMap
                    .FirstOrDefault(pair => pair.Value == selectedDisplayName).Key;
                setter(selectedEnum);
            },
            SelectedOption = enumToDisplayNameMap[getter()]
        };
        flexNode.Add(dropdownNode);
        return flexNode;
    }

    private SliderNode CreateSliderOption(int min, int max, int step, Func<int> getter, Action<int> setter) {
        SliderNode sliderNode = new() {
            X = OptionOffset,
            Width = CheckBoxWidth,
            Height = 30,
            IsVisible = true,
            Min = min,
            Max = max,
            Step = step,
            Value = getter(),
            OnValueChanged = setter
        };
        return sliderNode;
    }

    private IOverlayConfiguration GetOverlayByKind(NodeKind kind) {
        return kind switch {
            NodeKind.Combined => overlayManager.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
                "The provided NodeKind is not supported by GetOverlayByKind.")
        };
    }

    #endregion
}
