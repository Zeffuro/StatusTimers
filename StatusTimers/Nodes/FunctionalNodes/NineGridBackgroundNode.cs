using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class NineGridBackgroundNode : SimpleNineGridNode {
    public NineGridBackgroundNode() {
        TexturePath = "ui/uld/ToolTipS.tex";
        TextureCoordinates = new Vector2(0.0f, 0.0f);
        TextureSize = new Vector2(32.0f, 24.0f);
        TopOffset = 10;
        BottomOffset = 10;
        LeftOffset = 15;
        RightOffset = 15;
        PartsRenderType = 148;
        NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill;
        IsVisible = true;
    }
}
