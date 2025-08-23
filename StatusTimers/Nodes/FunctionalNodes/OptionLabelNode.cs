using KamiToolKit.Nodes;
using StatusTimers.Helpers;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class OptionLabelNode : TextNode {
    public OptionLabelNode(string label, bool offset = true) {
        X = offset ? 18 : 0;
        Width = 300;
        Height = TextStyles.OptionLabel.Height;
        FontSize = TextStyles.Defaults.FontSize;
        TextColor = TextStyles.OptionLabel.TextColor;
        TextOutlineColor = TextStyles.Defaults.OutlineColor;
        TextFlags = TextStyles.Defaults.Flags;
        IsVisible = true;
        String = label;
    }
}
