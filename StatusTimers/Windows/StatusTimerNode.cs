using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Layout;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : ResNode {
    public delegate void StatusNodeActionHandler(uint statusId, ulong? gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget);

    private TextNode _actorName;
    private ResNode _containerResNode;

    private StatusTimerOverlayConfig? _currentOverlayConfig;
    private StatusNodeLayoutConfig _layout;
    private IconImageNode _iconNode;

    private CastBarProgressBarNode _progressNode;
    private StatusInfo _statusInfo;
    private TextNode _statusName;
    private NodeBase _statusRemaining;

    private Dictionary<string, NodeBase> _nodeMap = new();

    public StatusTimerNode(StatusTimerOverlayConfig? initialOverlayConfig) {
        _currentOverlayConfig = initialOverlayConfig;
        //_currentOverlayConfig.StatusNodeLayout = new StatusNodeLayoutConfig();

        _layout = _currentOverlayConfig.StatusNodeLayout;

        _containerResNode = new ResNode {
            IsVisible = true,
            Width = _layout.RowWidth,
            Height = _layout.RowHeight,
        };
        GlobalServices.NativeController.AttachNode(_containerResNode, this);

        _iconNode = new IconImageNode {
            IsVisible = _currentOverlayConfig.ShowIcon,
            Width = _layout.IconAnchor.Width,
            Height = _layout.IconAnchor.Height,
            X = _layout.IconAnchor.OffsetX,
            Y = _layout.IconAnchor.OffsetY,
        };
        GlobalServices.NativeController.AttachNode(_iconNode, _containerResNode);

        _statusName = new TextNode {
            IsVisible = _currentOverlayConfig.ShowStatusName,
            Width = _layout.NameAnchor.Width,
            Height = _layout.NameAnchor.Height,
            FontSize = (uint)_currentOverlayConfig.StatusNameTextStyle.FontSize,
            X = _layout.NameAnchor.OffsetX,
            Y = _layout.NameAnchor.OffsetY,
            TextColor = _currentOverlayConfig.StatusNameTextStyle.TextColor,
            TextOutlineColor = _currentOverlayConfig.StatusNameTextStyle.TextOutlineColor,
            TextFlags = TextFlags.Edge,
            NodeFlags = NodeFlags.Clip
        };
        GlobalServices.NativeController.AttachNode(_statusName, _containerResNode);

        _progressNode = new CastBarProgressBarNode {
            IsVisible = _currentOverlayConfig.ShowProgress,
            Height = _layout.ProgressAnchor.Height,
            Width = _layout.ProgressAnchor.Width > 0 ? _layout.ProgressAnchor.Width : 200,
            X = _layout.ProgressAnchor.OffsetX,
            Y = _layout.ProgressAnchor.OffsetY,
            Progress = 1f,
        };
        GlobalServices.NativeController.AttachNode(_progressNode, _containerResNode);

        _actorName = new TextNode {
            IsVisible = _currentOverlayConfig.ShowActorName,
            Width = _layout.ActorNameAnchor.Width,
            Height = _layout.ActorNameAnchor.Height,
            FontSize = (uint)_currentOverlayConfig.ActorNameTextStyle.FontSize,
            X = _layout.ActorNameAnchor.OffsetX,
            Y = _layout.ActorNameAnchor.OffsetY,
            TextColor = _currentOverlayConfig.ActorNameTextStyle.TextColor,
            TextOutlineColor = _currentOverlayConfig.ActorNameTextStyle.TextOutlineColor,
            TextFlags = TextFlags.Edge
        };
        GlobalServices.NativeController.AttachNode(_actorName, _containerResNode);

        SetRemainingNode(_currentOverlayConfig);

        RegisterNodeMap();

        if (_currentOverlayConfig.ShowActorLetter || _currentOverlayConfig.AllowTargetActor) {
            _iconNode.AddEvent(AddonEventType.MouseClick, OnIconClicked);
            _iconNode.EventFlagsSet = true;
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
            UpdateValues(_currentOverlayConfig);
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

    public void ApplyOverlayConfig(StatusTimerOverlayConfig? config)
    {
        if (_currentOverlayConfig.Equals(config)) {
            return;
        }

        _currentOverlayConfig = config;

        bool needsRebuild =
            (_statusRemaining is TextNineGridNode) != config.ShowStatusRemainingBackground
            || !GetCurrentTextStyle(_statusRemaining)!.Equals(config.StatusRemainingTextStyle);

        if (needsRebuild) {
            SetRemainingNode(config);
        }

        ApplyStyle(_statusRemaining, config.StatusRemainingTextStyle);

        _iconNode.IsVisible = config.ShowIcon;

        _statusName.IsVisible = config.ShowStatusName;
        _progressNode.IsVisible = config.ShowProgress;
        _actorName.IsVisible = config.ShowActorName && _statusInfo.ActorName != null;

        UpdateValues(config);
    }

    private void ApplyStyle(NodeBase node, TextStyle style)
    {
        switch (node)
        {
            case TextNode tn:
                tn.FontSize = (uint)style.FontSize;
                tn.FontType = style.FontType;
                tn.TextColor = style.TextColor;
                tn.TextOutlineColor = style.TextOutlineColor;
                tn.TextFlags = style.TextFlags;
                break;
            case TextNineGridNode ngn:
                ngn.FontSize = style.FontSize;
                ngn.FontType = style.FontType;
                ngn.TextColor = style.TextColor;
                ngn.TextOutlineColor = style.TextOutlineColor;
                ngn.TextFlags = style.TextFlags;
                break;
        }
    }

    private void RegisterNodeMap() {
        _nodeMap["Icon"] = _iconNode;
        _nodeMap["Name"] = _statusName;
        _nodeMap["ActorName"] = _actorName;
        _nodeMap["Progress"] = _progressNode;
        if (_statusRemaining != null) {
            _nodeMap["Timer"] = _statusRemaining;
        }

        if (_currentOverlayConfig.ShowActorLetter || _currentOverlayConfig.AllowTargetActor) {
            _iconNode.AddEvent(AddonEventType.MouseClick, OnIconClicked);
            _iconNode.EventFlagsSet = true;
        }
    }

    private void UpdateLayoutOffsets() {
        _nodeMap["Icon"] = _iconNode;
        _nodeMap["Name"] = _statusName;
        _nodeMap["ActorName"] = _actorName;
        _nodeMap["Progress"] = _progressNode;
        if (_statusRemaining != null) {
            _nodeMap["Timer"] = _statusRemaining;
        }

        LayoutNode(_iconNode, _layout.IconAnchor, "Icon");
        LayoutNode(_statusName, _layout.NameAnchor, "Name");
        LayoutNode(_actorName, _layout.ActorNameAnchor, "ActorName");
        LayoutNode(_progressNode, _layout.ProgressAnchor, "Progress");
        if (_statusRemaining != null) {
            LayoutNode(_statusRemaining, _layout.TimerAnchor, "Timer");
        }
    }

    private (float x, float y) GetAnchorBasePosition(
    AnchorTarget anchorTo,
    string selfKey,
    AnchorAlignment alignment)
    {
        switch (anchorTo)
        {
            case AnchorTarget.ContainerLeft:
                return (0, 0);
            case AnchorTarget.ContainerRight:
                return (_containerResNode.Width, 0);
            case AnchorTarget.ContainerTop:
                return (0, 0);
            case AnchorTarget.ContainerBottom:
                return (0, _containerResNode.Height);
            case AnchorTarget.ContainerCenterH:
                return (_containerResNode.Width / 2, 0);
            case AnchorTarget.ContainerCenterV:
                return (0, _containerResNode.Height / 2);
            default:
                string anchorStr = anchorTo.ToString();
                foreach (var key in _nodeMap.Keys)
                {
                    if (anchorStr.StartsWith(key) && key != selfKey)
                    {
                        var node = _nodeMap[key];
                        string edge = anchorStr.Substring(key.Length);

                        // Determine if the anchoring node wants vertical center
                        bool wantsVerticalCenter = alignment.HasFlag(AnchorAlignment.VerticalCenter) || alignment.HasFlag(AnchorAlignment.Center);
                        // Determine if the anchoring node wants horizontal center (uncommon for anchors, but possible)
                        bool wantsHorizontalCenter = alignment.HasFlag(AnchorAlignment.HorizontalCenter) || alignment.HasFlag(AnchorAlignment.Center);

                        switch (edge)
                        {
                            case "Left":
                                return (node.X, node.Y + (wantsVerticalCenter ? node.Height / 2f : 0));
                            case "Right":
                                return (node.X + node.Width, node.Y + (wantsVerticalCenter ? node.Height / 2f : 0));
                            case "Top":
                                return (node.X + (wantsHorizontalCenter ? node.Width / 2f : 0), node.Y);
                            case "Bottom":
                                return (node.X + (wantsHorizontalCenter ? node.Width / 2f : 0), node.Y + node.Height);
                            default:
                                return (node.X, node.Y);
                        }
                    }
                }
                return (0, 0);
        }
    }

    private void LayoutNode(NodeBase node, StatusNodeAnchorConfig anchorConfig, string selfKey) {
        var (baseX, baseY) = GetAnchorBasePosition(anchorConfig.AnchorTo, selfKey, anchorConfig.Alignment);
        float x = baseX + anchorConfig.OffsetX;
        float y = baseY + anchorConfig.OffsetY;

        var alignment = anchorConfig.Alignment;

        if (alignment.HasFlag(AnchorAlignment.Right)) {
            x -= node.Width;
        }
        else if (alignment.HasFlag(AnchorAlignment.HorizontalCenter) || alignment.HasFlag(AnchorAlignment.Center)) {
            x -= node.Width / 2f;
        }

        if (alignment.HasFlag(AnchorAlignment.Bottom)) {
            y -= node.Height;
        }
        else if (alignment.HasFlag(AnchorAlignment.VerticalCenter) || alignment.HasFlag(AnchorAlignment.Center)) {
            y -= node.Height / 2f;
        }

        node.X = x;
        node.Y = y;
    }

    private void SetRemainingNode(StatusTimerOverlayConfig? config) {
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
                Width = _layout.TimerAnchor.Width,
                Height = _layout.TimerAnchor.Height,
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
                Width = _layout.TimerAnchor.Width,
                Height = _layout.TimerAnchor.Height,
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

    public void UpdateValues(StatusTimerOverlayConfig? config) {
        if (_statusInfo.Id != 0) {
            _iconNode.IconId = _statusInfo.IconId;
        }
        if (_statusInfo.Id == 0) {
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
                _actorName.Text = "";
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
            _currentOverlayConfig.AllowDismissStatus) {
            gameObjectToTargetId = _statusInfo.GameObjectId;
        }

        OnStatusNodeActionTriggered?.Invoke(
            _statusInfo.Id,
            gameObjectToTargetId,
            Kind,
            _currentOverlayConfig.AllowDismissStatus,
            _currentOverlayConfig.AllowTargetActor
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

    private TextStyle? GetCurrentTextStyle(NodeBase node)
    {
        // If _statusRemaining could be null, add a null check
        return node switch
        {
            TextNode text => new TextStyle
            {
                FontSize = (int)text.FontSize,
                FontType = text.FontType,
                TextColor = text.TextColor,
                TextOutlineColor = text.TextOutlineColor,
                TextFlags = text.TextFlags
            },
            TextNineGridNode nine => new TextStyle
            {
                FontSize = nine.FontSize,
                FontType = nine.FontType,
                TextColor = nine.TextColor,
                TextOutlineColor = nine.TextOutlineColor,
                TextFlags = nine.TextFlags
            },
            _ => null
        };
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
