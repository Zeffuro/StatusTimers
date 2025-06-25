using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Helpers;
using System;
using System.Linq;
using System.Numerics;
using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : ResNode {
    private TextNode _actorName;
    private ResNode _containerResNode;
    private IconImageNode _iconNode;

    private bool _lastShowStatusRemainingBackgroundState;
    private CastBarProgressBarNode _progressNode;
    private StatusInfo _statusInfo;
    private TextNode _statusName;
    private NodeBase _statusRemaining;

    public StatusTimerNode(StatusTimerOverlay<TKey> parent) {
        Parent = parent;
        _containerResNode = new ResNode {
            Position = Position,
            Size = Size,
            IsVisible = true
        };
        Services.NativeController.AttachNode(_containerResNode, this);

        _iconNode = new IconImageNode {
            Size = new Vector2(48, 64),
            IsVisible = true,
            EnableEventFlags = true
        };
        Services.NativeController.AttachNode(_iconNode, _containerResNode);

        _progressNode = new CastBarProgressBarNode {
            Height = 20,
            Progress = 1f,
            IsVisible = true,
            Width = 200
        };
        Services.NativeController.AttachNode(_progressNode, _containerResNode);

        _actorName = new TextNode {
            IsVisible = false,
            Width = 180,
            Height = 14,
            FontSize = 12,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge
        };

        Services.NativeController.AttachNode(_actorName, _containerResNode);

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

        Services.NativeController.AttachNode(_statusName, _containerResNode);

        _statusRemaining = new TextNode {
            IsVisible = true,
            Width = Parent.StatusRemainingTextStyle.Width,
            Height = 22,
            FontSize = 20,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge
        };
        Services.NativeController.AttachNode(_statusRemaining, _containerResNode);

        SetRemainingNode();

        if (Parent.ShowActorLetter || Parent.AllowTargetActor) {
            Services.Framework.RunOnTick(() => _iconNode.AddEvent(AddonEventType.MouseClick, e => StatusNodeClick(this, e)), delayTicks: 10);
        }

        UpdateValues();

        AddLabelTimeLine(this);
        AddKeyFrameTimeline(_containerResNode);
    }

    public NodeKind Kind { get; set; }
    public StatusTimerOverlay<TKey> Parent { get; }

    public NodeBase OuterContainer { get; set; }

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

            if (_statusRemaining != null) {
                _statusRemaining.X = value - _statusRemaining.Width;
            }
        }
    }

    private void SetRemainingNode() {
        bool shouldBeNineGrid = Parent.ShowStatusRemainingBackground;
        bool isCurrentlyNineGrid = _statusRemaining is TextNineGridNode;

        if (shouldBeNineGrid == isCurrentlyNineGrid && _statusRemaining != null) {
            return;
        }

        if (_statusRemaining != null) {
            Services.NativeController.DetachNode(_statusRemaining);
            _statusRemaining.Dispose();
            _statusRemaining = null;
        }

        if (shouldBeNineGrid) {
            _statusRemaining = new TextNineGridNode {
                IsVisible = Parent.ShowStatusRemaining,
                Width = Parent.StatusRemainingTextStyle.Width,
                Height = Parent.StatusRemainingTextStyle.Height,
                FontSize = Parent.StatusRemainingTextStyle.FontSize,
                FontType = Parent.StatusRemainingTextStyle.FontType,
                TextColor = Parent.StatusRemainingTextStyle.TextColor,
                TextOutlineColor = Parent.StatusRemainingTextStyle.TextOutlineColor,
                TextFlags = Parent.StatusRemainingTextStyle.TextFlags
            };
        }
        else {
            _statusRemaining = new TextNode {
                IsVisible = Parent.ShowStatusRemaining,
                Width = Parent.StatusRemainingTextStyle.Width,
                Height = Parent.StatusRemainingTextStyle.Height,
                FontSize = (uint)Parent.StatusRemainingTextStyle.FontSize,
                FontType = Parent.StatusRemainingTextStyle.FontType,
                TextColor = Parent.StatusRemainingTextStyle.TextColor,
                TextOutlineColor = Parent.StatusRemainingTextStyle.TextOutlineColor,
                TextFlags = Parent.StatusRemainingTextStyle.TextFlags
            };
        }

        Services.NativeController.AttachNode(_statusRemaining, _containerResNode);
        _statusRemaining.X = Width - _statusRemaining.Width;
    }

    public void ToggleEventFlags() {
        _iconNode.EventFlagsSet = !Parent.IsLocked;
        /*
        if (!Parent.IsLocked) {
            _iconNode.ClearEventFlags();
        }
        else {
            _iconNode.SetEventFlags();
        }
        */
    }

    public void UpdateValues() {
        if (Parent == null) {
            return;
        }

        _statusName.Text = _statusInfo.Name;

        _iconNode.IconId = _statusInfo.IconId;
        _iconNode.IsVisible = Parent.ShowIcon;
        _actorName.IsVisible = Parent.ShowActorName && _statusInfo.ActorName != null;
        _statusRemaining.IsVisible = Parent.ShowStatusRemaining;
        _statusName.IsVisible = Parent.ShowStatusName;
        _progressNode.IsVisible = Parent.ShowProgress;

        if (_statusInfo.IsPermanent || _statusInfo.RemainingSeconds <= 0) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
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

            if (_statusRemaining is TextNode textNode) {
                textNode.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
            }
            else if (_statusRemaining is TextNineGridNode textNineGridNode) {
                textNineGridNode.Label = $"{_statusInfo.RemainingSeconds:0.0}s";
            }
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
        Timeline labels = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddLabel(1, 10, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(20, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build();

        node.AddTimeline(labels);
    }

    private void AddKeyFrameTimeline(NodeBase node) {
        // Future Zeff, this always goes on a child
        Timeline keyFrames = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddFrame(1, scale: new Vector2(1.4f, 1.4f))
            .AddFrame(10, scale: new Vector2(0.9f, 0.9f))
            .AddFrame(20, scale: Vector2.One)
            .EndFrameSet()
            .Build();

        node.AddTimeline(keyFrames);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconNode.Dispose();
            _statusName.Dispose();
            _statusRemaining.Dispose();
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
