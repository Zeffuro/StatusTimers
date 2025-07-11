using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Factories;

public static class ConfigurationUIFactory
{
    public const float OptionOffset = 18;
    public const float CheckBoxHeight = 16;
    public const float CheckBoxWidth = 300;
    public const float ButtonHeight = 30;

    public static readonly Dictionary<GrowDirection, string> GrowDirectionMap = new() {
        { GrowDirection.DownRight, "Down and Right" },
        { GrowDirection.DownLeft, "Down and Left" },
        { GrowDirection.UpRight, "Up and Right" },
        { GrowDirection.UpLeft, "Up and Left" }
    };

    public static readonly Dictionary<FontType, string> FontMap = new() {
        { FontType.Axis, "Axis" },
        { FontType.Miedinger, "Miedinger" },
        { FontType.TrumpGothic, "Trump Gothic" },
        { FontType.Jupiter, "Jupiter" },
        { FontType.JupiterLarge, "Jupiter Large" }
    };

    public static CheckboxNode CreateCheckboxOption(string label, Func<bool> getter, Action<bool> setter)
    {
        return new CheckboxNode
        {
            X = OptionOffset,
            Width = CheckBoxWidth,
            Height = CheckBoxHeight,
            IsVisible = true,
            LabelText = label,
            IsChecked = getter(),
            OnClick = setter
        };
    }

    public static HorizontalFlexNode CreateTwoOptionsRow(NodeBase first, NodeBase second, float height)
    {
        return new HorizontalFlexNode
        {
            IsVisible = true,
            X = OptionOffset,
            Width = 600,
            Height = height,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        }.WithNodes(first, second);
    }

    public static HorizontalFlexNode CreateLabeledDropdown<TEnum>(
        string labelText,
        Func<TEnum> getter,
        Action<TEnum> setter,
        IReadOnlyDictionary<TEnum, string> enumToDisplayNameMap
    ) where TEnum : Enum
    {
        var flexNode = new HorizontalFlexNode
        {
            IsVisible = true,
            X = OptionOffset,
            Width = 280,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        flexNode.AddNode(new TextNode
        {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 20,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = labelText
        });

        flexNode.AddNode(new TextDropDownNode
        {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 140,
            Height = 24,
            MaxListOptions = 5,
            Options = enumToDisplayNameMap.Values.ToList(),
            OnOptionSelected = selectedDisplayName => {
                TEnum selectedEnum = enumToDisplayNameMap
                    .FirstOrDefault(pair => pair.Value == selectedDisplayName).Key;
                setter(selectedEnum);
            },
            SelectedOption = enumToDisplayNameMap.TryGetValue(getter(), out var sel) ? sel : enumToDisplayNameMap.Values.First()
        });
        return flexNode;
    }

    public static HorizontalFlexNode CreateLabeledNumericOption(string labelText, Func<int> getter, Action<int> setter)
    {
        var flexNode = new HorizontalFlexNode
        {
            IsVisible = true,
            X = OptionOffset,
            Width = 280,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        flexNode.AddNode(new TextNode
        {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 20,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = labelText
        });

        var numericNode = new NumericInputNode
        {
            X = OptionOffset,
            Width = 140,
            Height = CheckBoxHeight,
            IsVisible = true,
            Min = -99999,
            Max = 99999,
            Value = getter(),
            OnValueUpdate = setter
        };
        numericNode.ValueTextNode.Text = getter().ToString();
        flexNode.AddNode(numericNode);
        return flexNode;
    }

    public static SliderNode CreateSliderOption(int min, int max, int step, Func<int> getter, Action<int> setter)
    {
        return new SliderNode
        {
            X = OptionOffset,
            Width = 280,
            Height = 30,
            IsVisible = true,
            Min = min,
            Max = max,
            Step = step,
            Value = getter(),
            OnValueChanged = setter
        };
    }

    private static HorizontalFlexNode WithNodes(this HorizontalFlexNode flex, params NodeBase[] nodes)
    {
        foreach (var n in nodes) {
            flex.AddNode(n);
        }

        return flex;
    }
}
