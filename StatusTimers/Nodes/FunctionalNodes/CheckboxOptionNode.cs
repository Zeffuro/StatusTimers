using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using System;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class CheckboxOptionNode : HorizontalListNode {
    private readonly CheckboxNode _checkbox;

    public CheckboxOptionNode() {
        X = 18;
        Width = 300;
        Height = 24;
        IsVisible = true;

        _checkbox = new CheckboxNode {
            IsVisible = true,
            Width = 300,
            Height = 20
        };
        AddNode(_checkbox);
    }

    public ReadOnlySeString String {
        get => _checkbox.String;
        set => _checkbox.String = value;
    }

    public bool IsChecked {
        get => _checkbox.IsChecked;
        set => _checkbox.IsChecked = value;
    }

    public Action<bool>? OnClick {
        get => _checkbox.OnClick;
        set => _checkbox.OnClick = value;
    }
}
