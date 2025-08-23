using Dalamud.Game.Text.SeStringHandling;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class LabeledDropdownOptionNode<TEnum> : HorizontalFlexNode where TEnum : Enum
{
    private TextDropDownNode _dropDownNode;
    private OptionLabelNode _labelNode;

    public LabeledDropdownOptionNode(
        string labelText,
        Func<TEnum> getter,
        Action<TEnum> setter,
        IReadOnlyDictionary<TEnum, string> enumToDisplayNameMap)
    {
        IsVisible = true;
        X = 18;
        Width = 280;
        Height = 24;
        AlignmentFlags = FlexFlags.FitHeight;
        FitPadding = 4;

        _labelNode = new OptionLabelNode(labelText, false);
        AddNode(_labelNode);

        _dropDownNode = new TextDropDownNode
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
        };

        AddNode(_dropDownNode);
    }

    public SeString LabelText
    {
        get => _labelNode.SeString;
        set => _labelNode.SeString = value;
    }

    public Action<string>? OnOptionSelected
    {
        get => _dropDownNode.OnOptionSelected;
        set => _dropDownNode.OnOptionSelected = value;
    }

    public string? SelectedOption
    {
        get => _dropDownNode.SelectedOption;
        set => _dropDownNode.SelectedOption = value;
    }

    public List<string>? Options
    {
        get => _dropDownNode.Options;
        set => _dropDownNode.Options = value;
    }
}
