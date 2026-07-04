using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using System;
using System.Drawing;
using System.Numerics;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class BackgroundTextNode : ResNode
{
    private ReadOnlySeString _lastLayoutString = string.Empty;
    private Vector4 _backgroundColor = KnownColor.Black.Vector();

    public BackgroundTextNode()
    {
        BackgroundNode = new NineGridBackgroundNode {
            Position = Vector2.Zero,
            AddColor = KnownColor.Black.Vector3(),
            IsVisible = false
        };
        BackgroundNode.AttachNode(this);

        TextNode = new TextNode {
            TextFlags = TextFlags.Edge,
            FontType = FontType.Axis,
            FontSize = 20,
            AlignmentType = AlignmentType.Left,
            Position = Vector2.Zero
        };
        TextNode.AttachNode(this);
    }

    public NineGridBackgroundNode BackgroundNode { get; }
    public TextNode TextNode { get; }

    public ReadOnlySeString String {
        get => TextNode.String;
        set {
            if (TextNode.String.ToString() == value.ToString()) {
                return;
            }

            TextNode.String = value;
            UpdateLayout();
        }
    }

    public int FontSize {
        get => (int)TextNode.FontSize;
        set {
            TextNode.FontSize = (uint)value;
            RecalculateBackgroundSize();
        }
    }

    public FontType FontType {
        get => TextNode.FontType;
        set {
            TextNode.FontType = value;
            RecalculateBackgroundSize();
        }
    }

    public bool ShowBackground {
        get => BackgroundNode.IsVisible;
        set => BackgroundNode.IsVisible = value;
    }

    public Vector4 BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            BackgroundNode.Color = new Vector4(1.0f, 1.0f, 1.0f, value.W);
            BackgroundNode.AddColor = new Vector3(value.X, value.Y, value.Z);
        }
    }

    public Vector4 TextColor {
        get => TextNode.TextColor;
        set => TextNode.TextColor = value;
    }

    public Vector4 TextOutlineColor {
        get => TextNode.TextOutlineColor;
        set => TextNode.TextOutlineColor = value;
    }

    public TextFlags TextFlags {
        get => TextNode.TextFlags;
        set {
            TextNode.TextFlags = value;
            RecalculateBackgroundSize();
        }
    }

    public AlignmentType AlignmentType {
        get => TextNode.AlignmentType;
        set {
            TextNode.AlignmentType = value;
            RecalculateBackgroundSize();
        }
    }

    public Vector2 Padding { get; set; } = new(6.0f, 2.0f);

    public void UpdateLayout()
    {
        var currentString = TextNode.String;
        if (currentString.ToString() == _lastLayoutString.ToString()) {
            return;
        }

        _lastLayoutString = currentString;
        RecalculateBackgroundSize();
    }

    protected override void OnSizeChanged()
    {
        TextNode.Size = Size;
        RecalculateBackgroundSize();
    }

    private void RecalculateBackgroundSize()
    {
        var textSize = TextNode.GetTextDrawSize(considerScale: false);

        float textWidth = textSize.X > 0 ? textSize.X : 10.0f;
        float textHeight = textSize.Y > 0 ? textSize.Y : 10.0f;
        var backgroundSize = new Vector2(
            Math.Max(30.0f, textWidth + Padding.X * 2.0f),
            Math.Max(20.0f, textHeight + Padding.Y * 2.0f)
        );

        BackgroundNode.Size = backgroundSize;

        float backgroundX = TextNode.AlignmentType switch
        {
            AlignmentType.Center or AlignmentType.Top or AlignmentType.Bottom => (TextNode.Width - backgroundSize.X) / 2.0f,
            AlignmentType.Right or AlignmentType.TopRight or AlignmentType.BottomRight => TextNode.Width - backgroundSize.X + Padding.X,
            _ => -Padding.X
        };

        float backgroundY = TextNode.AlignmentType switch
        {
            AlignmentType.Bottom or AlignmentType.BottomLeft or AlignmentType.BottomRight => TextNode.Height - backgroundSize.Y + Padding.Y,
            AlignmentType.Top or AlignmentType.TopLeft or AlignmentType.TopRight => -Padding.Y,
            _ => (TextNode.Height - backgroundSize.Y) / 2.0f
        };

        BackgroundNode.Position = new Vector2(backgroundX, backgroundY);
    }
}
