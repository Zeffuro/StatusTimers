using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Helpers;
using StatusTimers.StatusSources;
using System;
using System.Linq;
using System.Numerics;
using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : ResNode {
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

        AddLabelTimeLine(this);
        AddKeyFrameTimeline(_iconNode);
    }

    public NodeKind Kind { get; set; }

    public StatusTimerOverlay<TKey> Parent {
        get;
        set {
            field = value;
            if (value.ShowActorLetter || value.AllowTargetActor) {
                _iconNode.AddEvent(AddonEventType.MouseClick, e => StatusNodeClick(this, e));
            }
        }
    }

    public StatusInfo StatusInfo {
        get => _statusInfo;
        set {
            _statusInfo = value;
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
        _iconNode.IsVisible = Parent.ShowIcon;
        _actorName.IsVisible = Parent.ShowActorName && _statusInfo.ActorName != null;
        _statusRemaining.IsVisible = Parent.ShowStatusRemaining;
        _statusRemainingBackground.IsVisible = Parent.ShowStatusRemainingBackground;
        _statusName.IsVisible = Parent.ShowStatusName;
        _progressNode.IsVisible = Parent.ShowProgress;

        if (_statusInfo.IsPermanent || _statusInfo.RemainingSeconds <= 0) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
            _statusRemainingBackground.IsVisible = false;
        }
        else {
            if (Math.Abs(_statusInfo.RemainingSeconds - _statusInfo.MaxSeconds) < 0.01 && Parent.AnimationsEnabled) {
                Timeline?.StartAnimation(10);
            }
            _progressNode.IsVisible = Parent.ShowProgress;
            _statusRemaining.IsVisible = Parent.ShowStatusRemaining;

            if (_statusInfo.ActorName != null && Parent.ShowActorName) {
                _actorName.Text = $"{(Parent.ShowActorLetter ? _statusInfo.EnemyLetter : "")}{_statusInfo.ActorName}";
            }

            float max = Math.Max(_statusInfo.MaxSeconds, 1f);
            float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
            float ratio = remaining / max;

            _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
            _statusRemainingBackground.X = _statusRemaining.X - 2;
            _statusRemainingBackground.Width = _statusRemaining.GetTextDrawSize(_statusRemaining.Text.TextValue).X + 14;
            _statusRemaining.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
        }
    }

    private unsafe void StatusNodeClick(StatusTimerNode<TKey> node, AddonEventData eventData) {
        AtkEventData* atkEventData = (AtkEventData*)eventData.AtkEventDataPointer;
        if (atkEventData->MouseData.ButtonId == 1 && node.Kind == NodeKind.Combined && Parent.AllowDismissStatus) {
            StatusManager.ExecuteStatusOff(node.StatusInfo.Id);
        }

        if (atkEventData->MouseData.ButtonId == 0 && node.Kind == NodeKind.MultiDoT && Parent.AllowTargetActor) {
            Services.TargetManager.Target =
                Services.ObjectTable.FirstOrDefault(o =>
                    o is not null && o.GameObjectId == node.StatusInfo.GameObjectId);
        }
    }

    private void AddLabelTimeLine(NodeBase node) {
        // Future Zeff, this always goes on a parent
        var labels = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddLabel(1, 10, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(20, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build();

        node.AddTimeline(labels);
    }
    private void AddKeyFrameTimeline(NodeBase node) {
        // Future Zeff, this always goes on a child
        var keyFrames = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddFrame(1, scale: new Vector2(1.4f, 1.4f))
            .AddFrame(10, scale: new Vector2(0.9f, 0.9f))
            .AddFrame(20, scale: Vector2.One)
            .EndFrameSet()
            .Build();

        node.AddTimeline(keyFrames);
    }

    // Whenever we inherit a node and add additional nodes,
    // we will be responsible for calling dispose on those nodes
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconNode.Dispose();
            _statusName.Dispose();
            _statusRemaining.Dispose();
            _statusRemainingBackground.Dispose();
            _actorName.Dispose();
            _progressNode.Dispose();

            base.Dispose(disposing);
        }
    }
}

public enum NodeKind {
    Combined,
    MultiDoT
}
