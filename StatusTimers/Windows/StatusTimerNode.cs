using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Timelines;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Extensions;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public sealed class StatusTimerNode<TKey> : SimpleOverlayNode {
    public delegate void StatusNodeActionHandler(uint statusId, ulong gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget);

    private SimpleOverlayNode _containerResNode;

    private readonly Func<StatusTimerOverlayConfig> _getOverlayConfig;

    private SimpleNineGridNode _statusBackgroundNode;
    private IconImageNode _iconNode;
    private ProgressBarCastNode _progressNode;
    private NodeBase _statusName;
    private NodeBase _statusRemaining;
    private NodeBase _actorName;

    private Dictionary<string, NodeBase> _nodeMap = new();
    private Dictionary<string, Vector2> _anchorCache = new();

    private StatusInfo? _lastStatusInfo;

    private bool _isDisposed;

    public unsafe StatusTimerNode(Func<StatusTimerOverlayConfig> getOverlayConfig) {
        _getOverlayConfig = getOverlayConfig;

        var config = _getOverlayConfig();

        _containerResNode = new SimpleOverlayNode() {
            IsVisible = true,
            Width = config.RowWidth,
            Height = config.RowHeight,
            Origin = new Vector2(15.0f, 30.0f)
        };

        // Background
        _statusBackgroundNode = new NineGridBackgroundNode {
        };
        ApplyNodeSettings(_statusBackgroundNode, config.Background);

        // Icon
        _iconNode = new IconImageNode { ImageNodeFlags = 0, WrapMode = WrapMode.Stretch };
        _iconNode.TextureSize = new Vector2(24, 32);
        ApplyNodeSettings(_iconNode, config.Icon);

        // Status Name
        _statusName = config.Name.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
        ApplyNodeSettings(_statusName, config.Name);
        ApplyTextStyle(_statusName, config.Name.Style);
        if (config.Name.Style != null) {
            config.Name.Style.Changed += OnStatusNameTextStyleChanged;
        }

        // Progress
        _progressNode = new ProgressBarCastNode();
        ApplyNodeSettings(_progressNode, config.Progress);
        ApplyBarStyle(_progressNode, config.Progress.StyleBar);
        if (config.Progress.StyleBar != null) {
            config.Progress.StyleBar.Changed += OnProgressBarStyleChanged;
        }

        // Actor Name
        _actorName = config.Actor.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
        ApplyNodeSettings(_actorName, config.Actor);
        ApplyTextStyle(_actorName, config.Actor.Style);
        if (config.Actor.Style != null) {
            config.Actor.Style.Changed += OnActorNameTextStyleChanged;
        }

        // Timer
        _statusRemaining = config.Timer.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
        ApplyNodeSettings(_statusRemaining, config.Timer);
        ApplyTextStyle(_statusRemaining, config.Timer.Style);
        if (config.Timer.Style != null) {
            config.Timer.Style.Changed += OnStatusRemainingTextStyleChanged;
        }

        RegisterNodeMap();

        if (config.ShowActorLetter || config.AllowTargetActor) {
            _iconNode.DrawFlags = DrawFlags.ClickableCursor;
            _iconNode.AddEvent(AtkEventType.MouseClick, OnIconClicked);
        }

        _containerResNode.AttachNode(this);

        _statusBackgroundNode.AttachNode(_containerResNode);
        _iconNode.AttachNode(_containerResNode);
        _statusName.AttachNode(_containerResNode);
        _progressNode.AttachNode(_containerResNode);
        _actorName.AttachNode(_containerResNode);
        _statusRemaining.AttachNode(_containerResNode);

        UpdateLayoutOffsets();

        AddLabelTimeLine(this);
        AddKeyFrameTimeline(_containerResNode);
    }

    public NodeKind Kind { get; set; }

    public required StatusInfo StatusInfo {
        get;
        set {
            field = value;
            UpdateValues();
        }
    }

    protected override void OnSizeChanged() {
        ClearAnchorCache();
        UpdateLayoutOffsets();
    }

    public event StatusNodeActionHandler? OnStatusNodeActionTriggered;

    public void ApplyOverlayConfig(string changedProperty) {
        var config = _getOverlayConfig();
        ClearAnchorCache();

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

        _actorName.IsVisible = config.Actor.IsVisible && StatusInfo.ActorName != null;

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

    private void ApplyBarStyle(ProgressBarCastNode node, StatusTimerOverlayConfig.BarStyle? style) {
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

    private unsafe void RegisterNodeMap() {
        _nodeMap["Background"] = _statusBackgroundNode;
        _nodeMap["Icon"] = _iconNode;
        _nodeMap["Name"] = _statusName;
        _nodeMap["ActorName"] = _actorName;
        _nodeMap["Progress"] = _progressNode;
        _nodeMap["Timer"] = _statusRemaining;

        var config = _getOverlayConfig();
        if (config.ShowActorLetter || config.AllowTargetActor) {
            _iconNode.AddEvent(AtkEventType.MouseClick, OnIconClicked);
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

    private void ClearAnchorCache() {
        _anchorCache.Clear();
    }

    private Vector2 GetAnchorBasePosition(
    AnchorTarget anchorTo,
    string selfKey,
    AnchorAlignment alignment)
    {
        string cacheKey = $"{anchorTo}-{selfKey}-{alignment}";
        if (_anchorCache.TryGetValue(cacheKey, out var cached)) {
            return cached;
        }

        // Handle container anchors
        switch (anchorTo)
        {
            case AnchorTarget.ContainerLeft:
                return _anchorCache[cacheKey] = new Vector2(0, 0);
            case AnchorTarget.ContainerRight:
                return _anchorCache[cacheKey] = new Vector2(_containerResNode.Width, 0);
            case AnchorTarget.ContainerTop:
                return _anchorCache[cacheKey] = new Vector2(0, 0);
            case AnchorTarget.ContainerBottom:
                return _anchorCache[cacheKey] = new Vector2(0, _containerResNode.Height);
            case AnchorTarget.ContainerCenterH:
                return _anchorCache[cacheKey] = new Vector2(_containerResNode.Width / 2, 0);
            case AnchorTarget.ContainerCenterV:
                return _anchorCache[cacheKey] = new Vector2(0, _containerResNode.Height / 2);
        }

        // Use the mapping for other anchors
        if (!AnchorTargetMap.Map.TryGetValue(anchorTo, out var mapping) || mapping.nodeKey == selfKey ||
            !_nodeMap.TryGetValue(mapping.nodeKey, out var node)) {
            return _anchorCache[cacheKey] = new Vector2(0, 0);
        }

        bool wantsVerticalCenter = alignment.HasFlag(AnchorAlignment.VerticalCenter) || alignment.HasFlag(AnchorAlignment.Center);
        bool wantsHorizontalCenter = alignment.HasFlag(AnchorAlignment.HorizontalCenter) || alignment.HasFlag(AnchorAlignment.Center);

        Vector2 result = mapping.edge switch
        {
            "Left"   => new Vector2(node.X, node.Y + (wantsVerticalCenter ? node.Height / 2f : 0)),
            "Right"  => new Vector2(node.X + node.Width, node.Y + (wantsVerticalCenter ? node.Height / 2f : 0)),
            "Top"    => new Vector2(node.X + (wantsHorizontalCenter ? node.Width / 2f : 0), node.Y),
            "Bottom" => new Vector2(node.X + (wantsHorizontalCenter ? node.Width / 2f : 0), node.Y + node.Height),
            _        => new Vector2(node.X, node.Y)
        };
        return _anchorCache[cacheKey] = result;
    }

    private void LayoutNode(NodeBase node, StatusTimerOverlayConfig.StatusNodeAnchorConfig anchorConfig, string selfKey) {
        var basePosition = GetAnchorBasePosition(anchorConfig.AnchorTo, selfKey, anchorConfig.Alignment);
        float x = basePosition.X + anchorConfig.OffsetX;
        float y = basePosition.Y + anchorConfig.OffsetY;

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
        if (rebuildName) {
            _statusName.DetachNode();
            _statusName.Dispose();
            _statusName = config.Name.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
            ApplyNodeSettings(_statusName, config.Name);
            ApplyTextStyle(_statusName, config.Name.Style);
            if (config.Name.Style != null) {
                config.Name.Style.Changed += OnStatusNameTextStyleChanged;
            }

            _containerResNode.AttachNode(_statusName);
        }
        if (rebuildActor) {
            _actorName.DetachNode();
            _actorName.Dispose();
            _actorName = config.Actor.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
            ApplyNodeSettings(_actorName, config.Actor);
            ApplyTextStyle(_actorName, config.Actor.Style);
            if (config.Actor.Style != null) {
                config.Actor.Style.Changed += OnActorNameTextStyleChanged;
            }

            _containerResNode.AttachNode(_actorName);
        }
        if (rebuildTimer) {
            _statusRemaining.DetachNode();
            _statusRemaining.Dispose();
            _statusRemaining = config.Timer.BackgroundEnabled == true ? new TextNineGridNode() : new TextNode();
            ApplyNodeSettings(_statusRemaining, config.Timer);
            ApplyTextStyle(_statusRemaining, config.Timer.Style);
            if (config.Timer.Style != null) {
                config.Timer.Style.Changed += OnStatusRemainingTextStyleChanged;
            }

            _containerResNode.AttachNode(_statusRemaining);
        }
        RegisterNodeMap();
    }

    public void UpdateValues() {
        if (_isDisposed) {
            return;
        }

        if (StatusInfo.Id == 0) {
            ResetStatusDisplay();
            return;
        }

        var config = _getOverlayConfig();

        // First time setup
        if (_lastStatusInfo == null || !StatusInfo.Equals(_lastStatusInfo)) {
            _iconNode.IsVisible = config.Icon.IsVisible;
            _iconNode.IconId = StatusInfo.IconId;
            _statusName.SetText(StatusInfo.Name);
            _statusName.IsVisible = config.Name.IsVisible;
            _statusBackgroundNode.IsVisible = config.Background.IsVisible;

            if (StatusInfo.ActorName != null && config.Actor.IsVisible) {
                _actorName.SetText($"{(config.ShowActorLetter ? StatusInfo.EnemyLetter : "")}{StatusInfo.ActorName}");
                _actorName.IsVisible = true;
            } else {
                _actorName.SetText(string.Empty);
                _actorName.IsVisible = false;
            }
        }

        if (StatusInfo.IsPermanent || StatusInfo.RemainingSeconds <= 0 || StatusInfo.MaxSeconds <= 0) {
            _progressNode.IsVisible = false;
            _progressNode.Progress = 0f;
            _statusRemaining.IsVisible = false;
            _statusRemaining.SetText(string.Empty);
        } else {
            if (Math.Abs(StatusInfo.RemainingSeconds - StatusInfo.MaxSeconds) < 0.01 && config.AnimationsEnabled) {
                Timeline?.PlayAnimation(10);
            }
            _progressNode.IsVisible = config.Progress.IsVisible;
            float progress = Math.Clamp(Helpers.Util.CalculateProgressRatio(StatusInfo.RemainingSeconds, StatusInfo.MaxSeconds), 0f, 1f);
            _progressNode.Progress = float.IsFinite(progress) ? progress : 0f;

            _statusRemaining.IsVisible = config.Timer.IsVisible;
            _statusRemaining.SetText(Helpers.Util.SafeFormatTime(StatusInfo.RemainingSeconds, config.TimerFormat));
        }

        _lastStatusInfo = StatusInfo;
    }

    private void ResetStatusDisplay()
    {
        _iconNode.IsVisible = false;
        _statusName.IsVisible = false;
        _statusRemaining.IsVisible = false;
        _progressNode.IsVisible = false;
        _actorName.IsVisible = false;

        _statusName.SetText(string.Empty);
        _statusRemaining.SetText(string.Empty);
        _progressNode.Progress = 0f;
        _actorName.SetText(string.Empty);
        _lastStatusInfo = null;
    }

    private void OnStatusNameTextStyleChanged() => ApplyTextStyle(_statusName, _getOverlayConfig().Name.Style);
    private void OnActorNameTextStyleChanged() => ApplyTextStyle(_actorName, _getOverlayConfig().Actor.Style);
    private void OnStatusRemainingTextStyleChanged() => ApplyTextStyle(_statusRemaining, _getOverlayConfig().Timer.Style);
    private void OnProgressBarStyleChanged() => ApplyBarStyle(_progressNode, _getOverlayConfig().Progress.StyleBar);
    private unsafe void OnIconClicked(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        var config = _getOverlayConfig();

        if (StatusInfo.Id == 0) {
            return;
        }

        bool shouldInvoke =
            (atkEventData->IsRightClick() && Kind == NodeKind.Combined) ||
            (atkEventData->IsLeftClick() && Kind == NodeKind.MultiDoT);

        if (!shouldInvoke) {
            return;
        }

        OnStatusNodeActionTriggered?.Invoke(
            StatusInfo.Id,
            StatusInfo.GameObjectId,
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
        var timeLineBuilder = new TimelineBuilder();
        var scaleAnimation = new FrameSetBuilder(timeLineBuilder, 1, 30)
            .AddFrame(1, scale: new Vector2(1.4f, 1.4f))
            .AddFrame(10, scale: new Vector2(0.9f, 0.9f))
            .AddFrame(30, scale: Vector2.One);

        var fadeAnimation = scaleAnimation
            .AddFrame(1, alpha: 175)
            .AddFrame(20, alpha: 255)
            .AddFrame(30, alpha: 255)
            .EndFrameSet();

        node.AddTimeline(fadeAnimation.Build());
    }

    private StatusTimerOverlayConfig.TextStyle? GetCurrentTextStyle(NodeBase node)
    {
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

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
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
        base.Dispose(disposing, isNativeDestructor);
    }

    public bool IsDisposed => _isDisposed;
}
