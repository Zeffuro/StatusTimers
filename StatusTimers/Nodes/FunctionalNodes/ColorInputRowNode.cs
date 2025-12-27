// File: StatusTimers/Nodes/FunctionalNodes/ColorInputRowNode.cs

using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using System;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class ColorInputRowNode : HorizontalListNode
{
    private readonly TextNode _labelNode;
    public NumericInputNode InputNode { get; }

    public ColorInputRowNode()
    {
        IsVisible = true;
        Width = 80;
        Height = 28;

        _labelNode = new TextNode
        {
            IsVisible = true,
            Y = 3,
            Width = 14,
            Height = 28,
            FontSize = 14,
        };
        AddNode(_labelNode);

        InputNode = new NumericInputNode
        {
            IsVisible = true,
            X = 14,
            Width = 80,
            Height = 28,
        };
        AddNode(InputNode);
    }

    public string Label
    {
        get => _labelNode.String;
        set => _labelNode.String = value;
    }

    public ReadOnlySeString LabelTooltip
    {
        get => _labelNode.TextTooltip;
        set => _labelNode.TextTooltip = value;
    }

    public int Value
    {
        get => InputNode.Value;
        set => InputNode.Value = value;
    }

    public int Min
    {
        get => InputNode.Min;
        set => InputNode.Min = value;
    }

    public int Max
    {
        get => InputNode.Max;
        set => InputNode.Max = value;
    }

    public Action<int>? OnValueUpdate
    {
        get => InputNode.OnValueUpdate;
        set => InputNode.OnValueUpdate = value;
    }
}
