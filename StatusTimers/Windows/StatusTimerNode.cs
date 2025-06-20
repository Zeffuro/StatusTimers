using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
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
    private readonly ProgressBarNode _progressNode;
    private readonly TextNode _statusName;
    private readonly TextNode _statusRemaining;
    private StatusInfo _statusInfo;

    public StatusTimerNode() {
        _iconNode = new IconImageNode {
            Size = new Vector2(48, 64),
            IsVisible = true,
            EnableEventFlags = true
        };
        Services.NativeController.AttachNode(_iconNode, this);

        _progressNode = new ProgressBarNode {
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

        Height = 60;
        Width = 300;
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

    public void UpdateValues() {
        _statusName.Text = _statusInfo.Name;
        _iconNode.IconId = _statusInfo.IconId;

        if (_statusInfo.IsPermanent) {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = false;
        }
        else {
            _progressNode.IsVisible = true;
            _statusRemaining.IsVisible = true;

            if (_statusInfo.ActorName != null)
                _actorName.Text = $"{_statusInfo.EnemyLetter}{_statusInfo.ActorName}";
            else
                _actorName.IsVisible = false;

            float max = Math.Max(_statusInfo.MaxSeconds, 1f);
            float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
            float ratio = remaining / max;

            _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
            _statusRemaining.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
        }

        UpdatePositions();
    }

    public void UpdatePositions() {
        int padding = 5;

        _statusName.X = _iconNode.Width + 5;

        _actorName.X = _iconNode.Width + 7;
        _progressNode.X = _iconNode.Width + 2;
        _statusRemaining.X = Width - _statusRemaining.Width - 5;

        _iconNode.Y = (Height - _iconNode.Height) / 2;
        _statusName.Y = _statusName.Height / 2;
        _actorName.Y = _statusName.Y + _progressNode.Height + padding;
        _progressNode.Y = _actorName.IsVisible
            ? _actorName.Y + _actorName.FontSize + padding
            : _statusName.Y + _statusName.FontSize + padding;
        _statusRemaining.Y = _statusRemaining.Height / 2;
    }

    private static unsafe void StatusNodeClick(StatusTimerNode node, AddonEventData eventData) {
        AtkEventData* atkEventData = (AtkEventData*)eventData.AtkEventDataPointer;
        if (atkEventData->MouseData.ButtonId == 1 && node.Kind == NodeKind.Combined)
            StatusManager.ExecuteStatusOff(node.StatusInfo.Id);

        if (atkEventData->MouseData.ButtonId == 0 && node.Kind == NodeKind.MultiDoT)
            Services.TargetManager.Target =
                Services.ObjectTable.FirstOrDefault(o =>
                    o is not null && o.GameObjectId == node.StatusInfo.GameObjectId);
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
