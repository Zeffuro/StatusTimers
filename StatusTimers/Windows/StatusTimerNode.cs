using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Enums;
using StatusTimers.Models;
using System;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : ResNode {
    public delegate void StatusNodeActionHandler(uint statusId, ulong? gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget);

    private TextNode _actorName;
    private ResNode _containerResNode;

    private StatusNodeDisplayConfig _currentDisplayConfig;
    private IconImageNode _iconNode;

    private CastBarProgressBarNode _progressNode;
    private StatusInfo _statusInfo;
    private TextNode _statusName;
    private NodeBase _statusRemaining;

    public StatusTimerNode(StatusNodeDisplayConfig initialDisplayConfig) {
        _currentDisplayConfig = initialDisplayConfig;

        _containerResNode = new ResNode {
            IsVisible = true
        };
        GlobalServices.NativeController.AttachNode(_containerResNode, this);

        _iconNode = new IconImageNode {
            Size = new Vector2(48, 64),
            IsVisible = _currentDisplayConfig.ShowIcon,
            EnableEventFlags = true
        };
        GlobalServices.NativeController.AttachNode(_iconNode, _containerResNode);

        _progressNode = new CastBarProgressBarNode {
            Height = 20,
            Progress = 1f,
            IsVisible = _currentDisplayConfig.ShowProgress,
            Width = 200
        };
        GlobalServices.NativeController.AttachNode(_progressNode, _containerResNode);

        _actorName = new TextNode {
            IsVisible = _currentDisplayConfig.ShowActorName,
            Width = 180,
            Height = 14,
            FontSize = 12,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge
        };
        GlobalServices.NativeController.AttachNode(_actorName, _containerResNode);

        _statusName = new TextNode {
            IsVisible = _currentDisplayConfig.ShowStatusName,
            Width = 180,
            Height = 22,
            FontSize = 20,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge,
            NodeFlags = NodeFlags.Clip
        };
        GlobalServices.NativeController.AttachNode(_statusName, _containerResNode);

        SetRemainingNode(_currentDisplayConfig);

        if (_currentDisplayConfig.ShowActorLetter || _currentDisplayConfig.AllowTargetActor) {
            GlobalServices.Framework.RunOnTick(() => _iconNode.AddEvent(AddonEventType.MouseClick, OnIconClicked),
                delayTicks: 10);
        }

        UpdateLayoutOffsets();

        AddLabelTimeLine(this);
        AddKeyFrameTimeline(_containerResNode);
    }

    public NodeKind Kind { get; set; }
    public NodeBase OuterContainer { get; set; }

    public StatusInfo StatusInfo {
        get => _statusInfo;
        set {
            _statusInfo = value;
            UpdateValues(_currentDisplayConfig);
        }
    }

    public override float Width {
        get => base.Width;
        set {
            base.Width = value;
            UpdateLayoutOffsets();
        }
    }

    public event StatusNodeActionHandler? OnStatusNodeActionTriggered;

    private void UpdateLayoutOffsets() {
        _statusName.X = _iconNode.Width + 4;
        _actorName.X = _iconNode.Width + 4;
        _actorName.Y = _statusName.Height;
        _progressNode.X = _iconNode.Width;
        _progressNode.Y = _statusName.Height + _actorName.Height;

        if (_statusRemaining != null) {
            _statusRemaining.X = Width - _statusRemaining.Width;
        }
    }

    private void SetRemainingNode(StatusNodeDisplayConfig config) {
        bool shouldBeNineGrid = config.ShowStatusRemainingBackground;
        bool isCurrentlyNineGrid = _statusRemaining is TextNineGridNode;

        if (shouldBeNineGrid == isCurrentlyNineGrid && _statusRemaining != null) {
            return;
        }

        if (_statusRemaining != null) {
            try {
                GlobalServices.NativeController.DetachNode(_statusRemaining);
            } catch { /* ignore safely or log */ }
            try {
                _statusRemaining.Dispose();
            } catch { /* ignore safely or log */ }
            _statusRemaining = null;
        }

        if (shouldBeNineGrid) {
            _statusRemaining = new TextNineGridNode {
                IsVisible = config.ShowStatusRemaining,
                Width = config.StatusRemainingTextStyle.Width,
                Height = config.StatusRemainingTextStyle.Height,
                FontSize = config.StatusRemainingTextStyle.FontSize,
                FontType = config.StatusRemainingTextStyle.FontType,
                TextColor = config.StatusRemainingTextStyle.TextColor,
                TextOutlineColor = config.StatusRemainingTextStyle.TextOutlineColor,
                TextFlags = config.StatusRemainingTextStyle.TextFlags
            };
        }
        else {
            _statusRemaining = new TextNode {
                IsVisible = config.ShowStatusRemaining,
                Width = config.StatusRemainingTextStyle.Width,
                Height = config.StatusRemainingTextStyle.Height,
                FontSize = (uint)config.StatusRemainingTextStyle.FontSize,
                FontType = config.StatusRemainingTextStyle.FontType,
                TextColor = config.StatusRemainingTextStyle.TextColor,
                TextOutlineColor = config.StatusRemainingTextStyle.TextOutlineColor,
                TextFlags = config.StatusRemainingTextStyle.TextFlags
            };
        }

        GlobalServices.NativeController.AttachNode(_statusRemaining, _containerResNode);
        _statusRemaining.X = Width - _statusRemaining.Width;
    }

    public void ToggleEventFlags(bool isLocked) {
        _iconNode.EventFlagsSet = !isLocked;
        /*
        if (!Parent.IsLocked) {
            _iconNode.ClearEventFlags();
        }
        else {
            _iconNode.SetEventFlags();
        }
        */
    }

    public void UpdateValues(StatusNodeDisplayConfig config) {
        if (_statusInfo.Id != 0) {
            _iconNode.IconId = _statusInfo.IconId; // NEW: Set the actual icon ID
        }
        if (_statusInfo.Id == 0) {
            // If no status info, make all relevant nodes invisible.
            _iconNode.IsVisible = false;
            _statusName.IsVisible = false;
            _statusRemaining.IsVisible = false;
            _progressNode.IsVisible = false;
            _actorName.IsVisible = false;
            return;
        }

        _statusName.Text = _statusInfo.Name;

        _iconNode.IsVisible = config.ShowIcon;
        _statusRemaining.IsVisible = config.ShowStatusRemaining;
        _statusName.IsVisible = config.ShowStatusName;
        _progressNode.IsVisible = config.ShowProgress;
        _actorName.IsVisible = config.ShowActorName && _statusInfo.ActorName != null;

        if (_statusInfo.IsPermanent || _statusInfo.RemainingSeconds <= 0) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
        }
        else {
            if (Math.Abs(_statusInfo.RemainingSeconds - _statusInfo.MaxSeconds) < 0.01 && config.AnimationsEnabled) {
                Timeline?.StartAnimation(10);
            }

            _progressNode.IsVisible = config.ShowProgress;
            _statusRemaining.IsVisible = config.ShowStatusRemaining;

            if (_statusInfo.ActorName != null && config.ShowActorName) {
                _actorName.Text = $"{(config.ShowActorLetter ? _statusInfo.EnemyLetter : "")}{_statusInfo.ActorName}";
            }
            else {
                _actorName.Text = ""; // Clear text if actor name is not visible
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

    private unsafe void OnIconClicked(AddonEventData eventData) {
        if (_statusInfo.Id == 0) {
            return;
        }

        AtkEventData* atkEventData = (AtkEventData*)eventData.AtkEventDataPointer;

        ulong? gameObjectToTargetId = null;

        if (atkEventData->MouseData.ButtonId == 1 && Kind == NodeKind.Combined &&
            _currentDisplayConfig.AllowDismissStatus) {
            gameObjectToTargetId = _statusInfo.GameObjectId;
        }

        OnStatusNodeActionTriggered?.Invoke(
            _statusInfo.Id,
            gameObjectToTargetId,
            Kind,
            _currentDisplayConfig.AllowDismissStatus,
            _currentDisplayConfig.AllowTargetActor
        );
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
            _containerResNode.Dispose();

            base.Dispose(disposing);
        }
    }
}
