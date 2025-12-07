using Dalamud.Game.Addon.Events.EventDataTypes;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public class ColorPreviewButtonNode : ButtonBase {
    private ColorPreviewNode _colorPreview;

    public ColorPreviewButtonNode() {
        _colorPreview = new ColorPreviewNode {
            IsVisible = true,
            Position = Vector2.Zero,
            Size = base.Size,
            NodeId = 3,
        };

        _colorPreview.AttachNode(this);
        //AttachNode(_colorPreview);

        LoadTimelines();

        InitializeComponentEvents();
    }

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
}
