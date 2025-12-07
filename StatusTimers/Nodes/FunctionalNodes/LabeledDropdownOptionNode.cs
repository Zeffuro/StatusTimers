using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class LabeledDropdownOptionNode<T> : HorizontalFlexNode
{
    private TextDropDownNode _dropDownNode;
    private OptionLabelNode _labelNode;

    public LabeledDropdownOptionNode(
        string labelText,
        Func<T> getter,
        Action<T> setter,
        IReadOnlyDictionary<T, string> displayMap)
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
            Options = displayMap.Values.ToList(),
            OnOptionSelected = selected => {
                T? selectedKey = displayMap.FirstOrDefault(x => x.Value == selected).Key;
                setter(selectedKey);
            },
            SelectedOption = displayMap[getter()]
        };

        AddNode(_dropDownNode);
    }

    public ReadOnlySeString LabelText
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
