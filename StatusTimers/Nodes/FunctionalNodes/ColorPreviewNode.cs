using Dalamud.Interface;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using System.Drawing;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public class ColorPreviewNode : ResNode {
    private readonly BackgroundImageNode _colorBackground;
    private readonly BackgroundImageNode _colorForeground;

    public ColorPreviewNode(NativeController nativeController) {
        base.Size = new Vector2(64, 64);

        _colorBackground = new BackgroundImageNode
        {
            IsVisible = true,
            Color = KnownColor.Black.Vector(),
            Size = new Vector2(52, 52),
            Position = new Vector2(6, 6),
            FitTexture = true
        };
        nativeController.AttachNode(_colorBackground, this);

        _colorForeground = new BackgroundImageNode
        {
            IsVisible = true,
            Color = KnownColor.White.Vector(),
            Size = new Vector2(48, 48),
            Position = new Vector2(8, 8),
            FitTexture = true
        };
        nativeController.AttachNode(_colorForeground, this);
    }

    public override Vector4 Color {
        get => _colorForeground.Color;
        set => _colorForeground.Color = value;
    }

    public override Vector2 Size {
        get => base.Size;
        set {
            base.Size = value;
            UpdateLayout();
        }
    }

    public BackgroundImageNode BackgroundNode => _colorBackground;
    public BackgroundImageNode ForegroundNode => _colorForeground;

    private void UpdateLayout() {
        float backgroundPadding = 6f;
        float foregroundPadding = 8f;

        Vector2 backgroundSize = base.Size - new Vector2(backgroundPadding * 2);
        Vector2 foregroundSize = base.Size - new Vector2(foregroundPadding * 2);

        _colorBackground.Size = backgroundSize;
        _colorBackground.Position = new Vector2(backgroundPadding, backgroundPadding);

        _colorForeground.Size = foregroundSize;
        _colorForeground.Position = new Vector2(foregroundPadding, foregroundPadding);
    }
}
