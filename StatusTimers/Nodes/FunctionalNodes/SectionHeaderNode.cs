using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class SectionHeaderNode : TextNode {
    public SectionHeaderNode(string text) {
        IsVisible = true;
        Width = 120;
        Height = TextStyles.Header.Height;
        FontSize = TextStyles.Defaults.FontSize;
        TextColor = TextStyles.Header.TextColor;
        TextOutlineColor = TextStyles.Defaults.OutlineColor;
        TextFlags = TextStyles.Defaults.Flags;
        AlignmentType = AlignmentType.Left;
        String = text;
    }
}
