using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using System;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class LabeledNumericOptionNode : HorizontalFlexNode
{
    public LabeledNumericOptionNode(string labelText, Func<int> getter, Action<int> setter, bool allowNegative = true)
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
            Min = allowNegative ? -99999 : 0,
            Max = 99999,
            Value = getter(),
            OnValueUpdate = setter
        };
        numericNode.ValueTextNode.String = getter().ToString();
        AddNode(numericNode);
    }
}
