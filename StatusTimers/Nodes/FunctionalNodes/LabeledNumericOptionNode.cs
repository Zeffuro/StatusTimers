using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using System;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class LabeledNumericOptionNode : HorizontalFlexNode
{
    public LabeledNumericOptionNode(string labelText, Func<int> getter, Action<int> setter)
    {
        IsVisible = true;
        X = 18;
        Width = 280;
        Height = 24;
        AlignmentFlags = FlexFlags.FitHeight;
        FitPadding = 4;

        AddNode(new OptionLabelNode(labelText));

        var numericNode = new NumericInputNode
        {
            X = 18,
            Width = 140,
            Height = 16,
            IsVisible = true,
            Min = -99999,
            Max = 99999,
            Value = getter(),
            OnValueUpdate = setter
        };
        numericNode.ValueTextNode.Text = getter().ToString();
        AddNode(numericNode);
    }
}
