using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class TwoOptionsRowNode : HorizontalFlexNode
{
    public TwoOptionsRowNode(NodeBase first, NodeBase second, float height)
    {
        IsVisible = true;
        X = 18;
        Width = 600;
        Height = height;
        AlignmentFlags = FlexFlags.CenterVertically;
        FitPadding = 4;

        AddNode(first);
        AddNode(second);
    }
}
