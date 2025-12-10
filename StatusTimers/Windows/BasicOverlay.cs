using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace StatusTimers.Windows;

public class BasicOverlay : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.AboveUserInterface;

    private TextNode textNode;
    public BasicOverlay() {
        textNode = new TextNode {
            String = "Hello, World!",
            FontSize = 24,
            Color = System.Numerics.Vector4.One,
        };
        textNode.AttachNode(this);
    }

    public override void Update() {
        base.Update();

        textNode.String = $"Current Time: {System.DateTime.Now:T}";
    }
}
