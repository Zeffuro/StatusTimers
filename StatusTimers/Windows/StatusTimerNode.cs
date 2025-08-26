using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Extensions;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Extensions;
using StatusTimers.Layout;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : ResNode {
    public delegate void StatusNodeActionHandler(uint statusId, ulong gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget);

    private ResNode _containerResNode;

    private readonly Func<StatusTimerOverlayConfig> _getOverlayConfig;

    private SimpleNineGridNode _statusBackgroundNode;
    private StatusInfo _statusInfo;
    private IconImageNode _iconNode;
    private CastBarProgressBarNode _progressNode;
    private NodeBase _statusName;
    private NodeBase _statusRemaining;
    private NodeBase _actorName;

    private Dictionary<string, NodeBase> _nodeMap = new();

    private bool _isDisposed = false;

    public StatusTimerNode(Func<StatusTimerOverlayConfig> getOverlayConfig) {
        _getOverlayConfig = getOverlayConfig;

        var config = _getOverlayConfig();

        _containerResNode = new ResNode {
            NodeId = 1,
            IsVisible = true,
            Width = config.RowWidth,
            Height = config.RowHeight,
            Origin = new Vector2(15.0f, 30.0f)
        };
        GlobalServices.NativeController.AttachNode(_containerResNode, this);

        // Background
        _statusBackgroundNode = new NineGridBackgroundNode {
            NodeId = 6,
        };
        ApplyNodeSettings(_statusBackgroundNode, config.Background);
        GlobalServices.NativeController.AttachNode(_statusBackgroundNode, _containerResNode);

        // Icon
        _iconNode = new IconImageNode { NodeId = 1 };
        ApplyNodeSettings(_iconNode, config.Icon);
        GlobalServices.NativeController.AttachNode(_iconNode, _containerResNode);

        // Status Name
        _statusName = config.Name.BackgroundEnabled == true ? new TextNineGridNode { NodeId = 2 } : new TextNode{ NodeId = 2 };
        ApplyNodeSettings(_statusName, config.Name);
        ApplyTextStyle(_statusName, config.Name.Style);
        if (config.Name.Style != null) {
            config.Name.Style.Changed += OnStatusNameTextStyleChanged;
        }
        GlobalServices.NativeController.AttachNode(_statusName, _containerResNode);

        // Progress
        _progressNode = new CastBarProgressBarNode { NodeId = 4 };
        ApplyNodeSettings(_progressNode, config.Progress);
        ApplyBarStyle(_progressNode, config.Progress.StyleBar);
        if (config.Progress.StyleBar != null) {
            config.Progress.StyleBar.Changed += OnProgressBarStyleChanged;
        }
        GlobalServices.NativeController.AttachNode(_progressNode, _containerResNode);

        // Actor Name
        _actorName = config.Actor.BackgroundEnabled == true ? new TextNineGridNode { NodeId = 3 } : new TextNode { NodeId = 3 };
        ApplyNodeSettings(_actorName, config.Actor);
        ApplyTextStyle(_actorName, config.Actor.Style);
        if (config.Actor.Style != null) {
            config.Actor.Style.Changed += OnActorNameTextStyleChanged;
        }
        GlobalServices.NativeController.AttachNode(_actorName, _containerResNode);

        // Timer
        _statusRemaining = config.Timer.BackgroundEnabled == true ? new TextNineGridNode{ NodeId = 5 } : new TextNode{ NodeId = 5 };
        ApplyNodeSettings(_statusRemaining, config.Timer);
        ApplyTextStyle(_statusRemaining, config.Timer.Style);
        if (config.Timer.Style != null) {
            config.Timer.Style.Changed += OnStatusRemainingTextStyleChanged;
        }
        GlobalServices.NativeController.AttachNode(_statusRemaining, _containerResNode);

        RegisterNodeMap();

        if (config.ShowActorLetter || config.AllowTargetActor) {
            _iconNode.AddEvent(AddonEventType.MouseClick, OnIconClicked);
            _iconNode.EventFlagsSet = true;
        }

        UpdateLayoutOffsets();

        AddLabelTimeLine(this);
        AddKeyFrameTimeline(_containerResNode);
    }

    public NodeKind Kind { get; set; }
    public NodeBase? OuterContainer { get; set; }

    public StatusInfo StatusInfo {
        get => _statusInfo;
        set {
            _statusInfo = value;
            UpdateValues();
        }
    }

    protected override void OnSizeChanged() {
        UpdateLayoutOffsets();
    }

    public event StatusNodeActionHandler? OnStatusNodeActionTriggered;

    public void ApplyOverlayConfig() {
        var config = _getOverlayConfig();

        bool needsRebuildName = (_statusName is TextNineGridNode) != (config.Name.BackgroundEnabled == true);
        bool needsRebuildActor = (_actorName is TextNineGridNode) != (config.Actor.BackgroundEnabled == true);
        bool needsRebuildTimer = (_statusRemaining is TextNineGridNode) != (config.Timer.BackgroundEnabled == true);

        if (needsRebuildName || needsRebuildActor || needsRebuildTimer) {
            RebuildNodes(config, needsRebuildName, needsRebuildActor, needsRebuildTimer);
        }

        ApplyNodeSettings(_statusBackgroundNode, config.Background);
        ApplyNodeSettings(_iconNode, config.Icon);
        ApplyNodeSettings(_statusName, config.Name);
        ApplyTextStyle(_statusName, config.Name.Style);

        ApplyNodeSettings(_progressNode, config.Progress);
        ApplyBarStyle(_progressNode, config.Progress.StyleBar);

        ApplyNodeSettings(_actorName, config.Actor);
        ApplyTextStyle(_actorName, config.Actor.Style);

        UpdateLayoutOffsets();

        _actorName.IsVisible = config.Actor.IsVisible && _statusInfo.ActorName != null;

        UpdateValues();
    }

    private void ApplyTextStyle(NodeBase node, StatusTimerOverlayConfig.TextStyle? style) {
        if (style == null) {
            return;
        }

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

    private void ApplyBarStyle(CastBarProgressBarNode node, StatusTimerOverlayConfig.BarStyle? style) {
        if (style == null) {
            return;
        }

        node.BackgroundColor = style.BackgroundColor;
        node.BarColor = style.ProgressColor;
        node.BorderColor = style.BorderColor;
        node.BorderVisible = style.BorderVisible;
    }

    private void ApplyNodeSettings(NodeBase node, StatusTimerOverlayConfig.NodePartConfig partConfig) {
        node.IsVisible = partConfig.IsVisible;
        node.Width = partConfig.Anchor.Width;
        node.Height = partConfig.Anchor.Height;
        node.X = partConfig.Anchor.OffsetX;
        node.Y = partConfig.Anchor.OffsetY;
    }

    private void RegisterNodeMap() {
        _nodeMap["Background"] = _statusBackgroundNode;
        _nodeMap["Icon"] = _iconNode;
        _nodeMap["Name"] = _statusName;
        _nodeMap["ActorName"] = _actorName;
        _nodeMap["Progress"] = _progressNode;
        _nodeMap["Timer"] = _statusRemaining;

        var config = _getOverlayConfig();
        if (config.ShowActorLetter || config.AllowTargetActor) {
            _iconNode.AddEvent(AddonEventType.MouseClick, OnIconClicked);
            _iconNode.EventFlagsSet = true;
        }
    }

    private void UpdateLayoutOffsets() {
        var config = _getOverlayConfig();
        _nodeMap["Background"] = _statusBackgroundNode;
        _nodeMap["Icon"] = _iconNode;
        _nodeMap["Name"] = _statusName;
        _nodeMap["ActorName"] = _actorName;
        _nodeMap["Progress"] = _progressNode;
        _nodeMap["Timer"] = _statusRemaining;

        LayoutNode(_statusBackgroundNode, config.Background.Anchor, "Background");
        LayoutNode(_iconNode, config.Icon.Anchor, "Icon");
        LayoutNode(_statusName, config.Name.Anchor, "Name");
        LayoutNode(_actorName, config.Actor.Anchor, "ActorName");
        LayoutNode(_progressNode, config.Progress.Anchor, "Progress");
        LayoutNode(_statusRemaining, config.Timer.Anchor, "Timer");
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

                        bool wantsVerticalCenter = alignment.HasFlag(AnchorAlignment.VerticalCenter) || alignment.HasFlag(AnchorAlignment.Center);
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

    private void LayoutNode(NodeBase node, StatusTimerOverlayConfig.StatusNodeAnchorConfig anchorConfig, string selfKey) {
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

    private void RebuildNodes(StatusTimerOverlayConfig config, bool rebuildName, bool rebuildActor, bool rebuildTimer)
    {
        // Remove and dispose old nodes if necessary
        if (rebuildName) {
            GlobalServices.NativeController.DetachNode(_statusName);
            _statusName.Dispose();
            _statusName = config.Name.BackgroundEnabled == true ? new TextNineGridNode{ NodeId = 2 } : new TextNode{ NodeId = 2 };
            ApplyNodeSettings(_statusName, config.Name);
            ApplyTextStyle(_statusName, config.Name.Style);
            if (config.Name.Style != null) {
                config.Name.Style.Changed += OnStatusNameTextStyleChanged;
            }

            GlobalServices.NativeController.AttachNode(_statusName, _containerResNode);
        }
        if (rebuildActor) {
            GlobalServices.NativeController.DetachNode(_actorName);
            _actorName.Dispose();
            _actorName = config.Actor.BackgroundEnabled == true ? new TextNineGridNode{ NodeId = 3 } : new TextNode{ NodeId = 3 };
            ApplyNodeSettings(_actorName, config.Actor);
            ApplyTextStyle(_actorName, config.Actor.Style);
            if (config.Actor.Style != null) {
                config.Actor.Style.Changed += OnActorNameTextStyleChanged;
            }

            GlobalServices.NativeController.AttachNode(_actorName, _containerResNode);
        }
        if (rebuildTimer) {
            GlobalServices.NativeController.DetachNode(_statusRemaining);
            _statusRemaining.Dispose();
            _statusRemaining = config.Timer.BackgroundEnabled == true ? new TextNineGridNode{ NodeId = 5 } : new TextNode{ NodeId = 5 };
            ApplyNodeSettings(_statusRemaining, config.Timer);
            ApplyTextStyle(_statusRemaining, config.Timer.Style);
            if (config.Timer.Style != null) {
                config.Timer.Style.Changed += OnStatusRemainingTextStyleChanged;
            }

            GlobalServices.NativeController.AttachNode(_statusRemaining, _containerResNode);
        }
        RegisterNodeMap();
    }

    public void UpdateValues() {
        if (_isDisposed) {
            return;
        }
        var config = _getOverlayConfig();

        if (_statusInfo.Id == 0) {
            _iconNode.IsVisible = false;
            _statusName.IsVisible = false;
            _statusRemaining.IsVisible = false;
            _progressNode.IsVisible = false;
            _actorName.IsVisible = false;
            return;
        }
        _iconNode.IconId = _statusInfo.Id > 0 ? _statusInfo.IconId : 0;

        _statusName.SetText(_statusInfo.Name);

        _statusBackgroundNode.IsVisible = config.Background.IsVisible;
        _iconNode.IsVisible = config.Icon.IsVisible;
        _statusRemaining.IsVisible = config.Timer.IsVisible;
        _statusName.IsVisible = config.Name.IsVisible;
        _progressNode.IsVisible = config.Progress.IsVisible;
        _actorName.IsVisible = config.Actor.IsVisible && _statusInfo.ActorName != null;

        if (_statusInfo.IsPermanent || _statusInfo.RemainingSeconds <= 0) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
        }
        else {
            if (Math.Abs(_statusInfo.RemainingSeconds - _statusInfo.MaxSeconds) < 0.01 && config.AnimationsEnabled) {
                Timeline?.PlayAnimation(10);
            }

            _progressNode.IsVisible = config.Progress.IsVisible;
            _statusRemaining.IsVisible = config.Timer.IsVisible;

            if (_statusInfo.ActorName != null && config.Actor.IsVisible) {
                _actorName.SetText($"{(config.ShowActorLetter ? _statusInfo.EnemyLetter : "")}{_statusInfo.ActorName}");
            }
            else {
                _actorName.SetText("");
            }

            if (_statusInfo.MaxSeconds > 0)
            {
                float max = Math.Max(_statusInfo.MaxSeconds, 1f);
                float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
                float ratio = remaining / max;
                _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
            }
            else {
                _progressNode.Progress = 0.06f;
            }


            _statusRemaining.SetText(Helpers.Util.SafeFormatTime(_statusInfo.RemainingSeconds, config.TimerFormat));
        }
    }

    private void OnStatusNameTextStyleChanged() => ApplyTextStyle(_statusName, _getOverlayConfig().Name.Style);
    private void OnActorNameTextStyleChanged() => ApplyTextStyle(_actorName, _getOverlayConfig().Actor.Style);
    private void OnStatusRemainingTextStyleChanged() => ApplyTextStyle(_statusRemaining, _getOverlayConfig().Timer.Style);
    private void OnProgressBarStyleChanged() => ApplyBarStyle(_progressNode, _getOverlayConfig().Progress.StyleBar);

    private unsafe void OnIconClicked(AddonEventData eventData) {
        var config = _getOverlayConfig();

        if (_statusInfo.Id == 0) {
            return;
        }

        AtkEventData* atkEventData = (AtkEventData*)eventData.AtkEventDataPointer;

        bool shouldInvoke =
            (atkEventData->MouseData.ButtonId == 1 && Kind == NodeKind.Combined) ||
            (atkEventData->MouseData.ButtonId == 0 && Kind == NodeKind.MultiDoT);

        if (!shouldInvoke) {
            return;
        }

        OnStatusNodeActionTriggered?.Invoke(
            _statusInfo.Id,
            _statusInfo.GameObjectId,
            Kind,
            config.AllowDismissStatus,
            config.AllowTargetActor
        );
    }

    private void AddLabelTimeLine(NodeBase node) {
        // Future Zeff, this always goes on a parent
        Timeline labels = new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddLabel(1, 10, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(30, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build();

        node.AddTimeline(labels);
    }

    private void AddKeyFrameTimeline(NodeBase node) {
        // Future Zeff, this always goes on a child
        Timeline keyFrames = new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddFrame(1, scale: new Vector2(1.4f, 1.4f))
            .AddFrame(1, alpha: 175)
            .AddFrame(10, scale: new Vector2(0.9f, 0.9f))
            .AddFrame(20, alpha: 255)
            .AddFrame(30, scale: Vector2.One)
            .AddFrame(30, alpha: 255)
            .EndFrameSet()
            .Build();

        node.AddTimeline(keyFrames);
    }

    private StatusTimerOverlayConfig.TextStyle? GetCurrentTextStyle(NodeBase node)
    {
        // If _statusRemaining could be null, add a null check
        return node switch
        {
            TextNode text => new StatusTimerOverlayConfig.TextStyle
            {
                FontSize = (int)text.FontSize,
                FontType = text.FontType,
                TextColor = text.TextColor,
                TextOutlineColor = text.TextOutlineColor,
                TextFlags = text.TextFlags
            },
            TextNineGridNode nine => new StatusTimerOverlayConfig.TextStyle
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
        if (_isDisposed) {
            return;
        }

        _isDisposed = true;

        if (disposing) {
            if (_getOverlayConfig().Name.Style is { } style) {
                style.Changed -= OnStatusNameTextStyleChanged;
            }
            if (_getOverlayConfig().Actor.Style is { } actorStyle) {
                actorStyle.Changed -= OnActorNameTextStyleChanged;
            }
            if (_getOverlayConfig().Progress.StyleBar is { } barStyle) {
                barStyle.Changed -= OnProgressBarStyleChanged;
            }
            if (_getOverlayConfig().Timer.Style is { } timerStyle) {
                timerStyle.Changed -= OnStatusRemainingTextStyleChanged;
            }
            _iconNode.Dispose();
            _statusName.Dispose();
            _statusRemaining.Dispose();
            _actorName.Dispose();
            _progressNode.Dispose();
            _containerResNode.Dispose();
        }
        base.Dispose(disposing);
    }

    public bool IsDisposed => _isDisposed;
}
