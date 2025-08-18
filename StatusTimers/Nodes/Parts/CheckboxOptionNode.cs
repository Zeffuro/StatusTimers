using KamiToolKit.Nodes;
using System;

namespace StatusTimers.Nodes.Parts;

public sealed class CheckboxOptionNode : CheckboxNode {
    public CheckboxOptionNode(string label) {
        X = 18;
        Width = 300;
        Height = 16;
        IsVisible = true;
        LabelText = label;
    }
}
