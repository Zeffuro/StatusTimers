using Dalamud.Interface;
using KamiToolKit.Classes;
using System.Drawing;
using System.IO;
using System.Numerics;
using KamiToolKit.Nodes;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.FunctionalNodes;

public class ColorPreviewNode : ResNode
{
    private readonly BackgroundImageNode _colorBackground;
    private readonly ImGuiImageNode _alphaLayer;
    private readonly BackgroundImageNode _colorForeground;

    public ColorPreviewNode()
    {
        base.Size = new Vector2(64, 64);

        _colorBackground = new BackgroundImageNode
        {
            IsVisible = true,
            Color = KnownColor.Black.Vector(),
            FitTexture = true,
        };
        _colorBackground.AttachNode(this);

        _alphaLayer = new ImGuiImageNode
        {
            IsVisible = true,
            TexturePath = GetAlphaTexturePath(),
            WrapMode = WrapMode.Tile,
        };
        _alphaLayer.AttachNode(this);

        _colorForeground = new BackgroundImageNode
        {
            IsVisible = true,
            Color = KnownColor.White.Vector(),
            FitTexture = true,
        };
        _colorForeground.AttachNode(this);

        UpdateLayout();
    }

    public override Vector4 Color
    {
        get => _colorForeground.Color;
        set => _colorForeground.Color = value;
    }

    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            UpdateLayout();
        }
    }

    public BackgroundImageNode BackgroundNode => _colorBackground;
    public BackgroundImageNode ForegroundNode => _colorForeground;

    private void UpdateLayout()
    {
        const float backgroundPadding = 6f;
        const float alphaPadding = 8f;
        const float foregroundPadding = 8f;

        var bgSize = base.Size - new Vector2(backgroundPadding * 2f);
        var alphaSize = base.Size - new Vector2(alphaPadding * 2f);
        var fgSize = base.Size - new Vector2(foregroundPadding * 2f);

        _colorBackground.Size = bgSize;
        _colorBackground.Position = new Vector2(backgroundPadding, backgroundPadding);

        _alphaLayer.Size = alphaSize;
        _alphaLayer.Position = new Vector2(alphaPadding, alphaPadding);

        _colorForeground.Size = fgSize;
        _colorForeground.Position = new Vector2(foregroundPadding, foregroundPadding);
    }

    private static string GetAlphaTexturePath()
    {
        var baseDir = GlobalServices.PluginInterface.AssemblyLocation.Directory!.FullName;
        return Path.Combine(baseDir, "Media", "Textures", "alpha_background.png");
    }
}
