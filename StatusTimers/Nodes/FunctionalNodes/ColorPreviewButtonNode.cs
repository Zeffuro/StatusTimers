using Dalamud.Game.Addon.Events.EventDataTypes;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
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
        //AttachNode(_colorPreview);

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

    private void ClickHandler(AddonEventData data) {
        OnClick?.Invoke();
    }

    private void LoadTimelines()
        => LoadTwoPartTimelines(this, _colorPreview);

    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (_isNodeDisposed)
        {
            base.Dispose(disposing, isNativeDestructor);
            return;
        }
        _isNodeDisposed = true;
        if (disposing)
        {
            _colorPreview.Dispose();
        }
        base.Dispose(disposing, isNativeDestructor);
    }
}
