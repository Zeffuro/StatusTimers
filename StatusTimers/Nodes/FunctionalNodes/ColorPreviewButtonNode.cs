using Dalamud.Game.Addon.Events.EventDataTypes;
using KamiToolKit.Nodes;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public class ColorPreviewButtonNode : ButtonBase {
    private ColorPreviewNode _colorPreview;
    private bool _isNodeDisposed;

    public ColorPreviewButtonNode() {
        _colorPreview = new ColorPreviewNode {
            IsVisible = true,
            Position = Vector2.Zero,
            Size = base.Size,
        };

        _colorPreview.AttachNode(this);

        LoadTimelines();

        InitializeComponentEvents();
    }

    public bool IsNodeDisposed => _isNodeDisposed;

    public override Vector4 Color
    {
        get => _colorPreview.Color;
        set => _colorPreview.Color = value;
    }

    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            _colorPreview.Size = value;
        }
    }

    private void LoadTimelines()
        => LoadTwoPartTimelines(this, _colorPreview);

    protected override void Dispose(bool isNativeDestructor)
    {
        if (_isNodeDisposed)
        {
            base.Dispose(isNativeDestructor);
            return;
        }
        _isNodeDisposed = true;
        _colorPreview.Dispose();
        base.Dispose(isNativeDestructor);
    }
}
