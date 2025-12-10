using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public class HUDLayoutBackgroundNode : NineGridNode {
    public HUDLayoutBackgroundNode() {
        BottomOffset = 8;
        TopOffset = 21;
        LeftOffset = 21;
        RightOffset = 21;

        AddPart(
            new Part {
                TexturePath = "ui/uld/HUDLayout.tex",
                Size = new Vector2(44, 32),
                TextureCoordinates = new Vector2(0, 0)
            },
            new Part {
                TexturePath = "ui/uld/HUDLayout.tex",
                Size = new Vector2(88, 16),
                TextureCoordinates = new Vector2(0, 16)
            },
            new Part {
                TexturePath = "ui/uld/HUDLayout.tex",
                Size = new Vector2(156, 80),
                TextureCoordinates = new Vector2(0, 24)
            }
        );
    }
}
