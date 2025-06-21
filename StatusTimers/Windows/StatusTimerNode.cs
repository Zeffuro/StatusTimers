using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using System;
using System.Linq;
using System.Numerics;
using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode : ResNode {
    private readonly TextNode _actorName;
    private readonly IconImageNode _iconNode;
    private readonly CastBarProgressBarNode _progressNode;
    private readonly TextNode _statusName;
    private readonly SimpleNineGridNode _statusRemainingBackground;
    private readonly TextNode _statusRemaining;
    private StatusInfo _statusInfo;

    public StatusTimerNode() {
        _iconNode = new IconImageNode {
            Size = new Vector2(48, 64),
            IsVisible = true,
            EnableEventFlags = true
        };
        Services.NativeController.AttachNode(_iconNode, this);

        _progressNode = new CastBarProgressBarNode() {
            Height = 20,
            Progress = 1f,
            IsVisible = true,
            Width = 200
        };
        Services.NativeController.AttachNode(_progressNode, this);

        _actorName = new TextNode {
            IsVisible = _statusInfo.ActorName != null,
            Width = 180,
            Height = 14,
            FontSize = 12,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge
        };

        Services.NativeController.AttachNode(_actorName, this);

        _statusName = new TextNode {
            IsVisible = true,
            Width = 180,
            Height = 22,
            FontSize = 20,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge,
            NodeFlags = NodeFlags.Clip
        };

        Services.NativeController.AttachNode(_statusName, this);


        _statusRemainingBackground = new SimpleNineGridNode {
            IsVisible = true,
            Width = 120,
            Height = 24,
            TexturePath = "ui/uld/ToolTipS.tex",
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(32, 24),
            BottomOffset = 11,
            TopOffset = 11,
            LeftOffset = 12,
            RightOffset = 12,
        };

        Services.NativeController.AttachNode(_statusRemainingBackground, this);

        _statusRemaining = new TextNode {
            IsVisible = true,
            Width = 120,
            Height = 22,
            FontSize = 20,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge
        };
        Services.NativeController.AttachNode(_statusRemaining, this);

        _iconNode.AddEvent(AddonEventType.MouseClick, e => StatusNodeClick(this, e));
    }

    public NodeKind Kind { get; set; }

    public StatusInfo StatusInfo {
        get => _statusInfo;
        set {
            _statusInfo = value;
            _actorName.IsVisible = _statusInfo.ActorName != null;
            UpdateValues();
        }
    }

    public override float Width {
        get => base.Width;
        set {
            base.Width = value;

            _statusName.X = _iconNode.Width + 4;
            _actorName.X = _iconNode.Width + 4;
            _actorName.Y = _statusName.Height;
            _progressNode.X = _iconNode.Width;
            _progressNode.Y = _statusName.Height + _actorName.Height;
            _statusRemaining.X = value - _statusRemaining.Width;
        }
    }

    public void UpdateValues() {
        _statusName.Text = _statusInfo.Name;
        _iconNode.IconId = _statusInfo.IconId;

        if (_statusInfo.IsPermanent || _statusInfo.RemainingSeconds < 0) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
            _statusRemainingBackground.IsVisible = false;
        }
        else {
            _progressNode.IsVisible = true;
            _statusRemaining.IsVisible = true;

            if (_statusInfo.ActorName != null) {
                _actorName.Text = $"{_statusInfo.EnemyLetter}{_statusInfo.ActorName}";
            }
            else {
                _actorName.IsVisible = false;
            }

            float max = Math.Max(_statusInfo.MaxSeconds, 1f);
            float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
            float ratio = remaining / max;

            _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
            _statusRemainingBackground.X = _statusRemaining.X;
            _statusRemainingBackground.Width = _statusRemaining.GetTextDrawSize(_statusRemaining.Text.TextValue).X + 14;
            _statusRemaining.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
        }
    }

    private static unsafe void StatusNodeClick(StatusTimerNode node, AddonEventData eventData) {
        AtkEventData* atkEventData = (AtkEventData*)eventData.AtkEventDataPointer;
        if (atkEventData->MouseData.ButtonId == 1 && node.Kind == NodeKind.Combined) {
            StatusManager.ExecuteStatusOff(node.StatusInfo.Id);
        }

        if (atkEventData->MouseData.ButtonId == 0 && node.Kind == NodeKind.MultiDoT) {
            Services.TargetManager.Target =
                Services.ObjectTable.FirstOrDefault(o =>
                    o is not null && o.GameObjectId == node.StatusInfo.GameObjectId);
        }
    }

    // Whenever we inherit a node and add additional nodes,
    // we will be responsible for calling dispose on those nodes
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconNode.Dispose();
            _statusName.Dispose();
            _statusRemaining.Dispose();

            base.Dispose(disposing);
        }
    }
}

public enum NodeKind {
    Combined,
    MultiDoT
}
