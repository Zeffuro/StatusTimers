using System;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;

namespace StatusTimers.Windows;

public class StatusTimerNode : ResNode {
    private readonly TextNode _statusName;
    private readonly TextNode _statusRemaining;
    private readonly ProgressBarNode _progressNode;
    private readonly IconImageNode _iconNode;
    private StatusInfo _statusInfo;

    public StatusTimerNode()
    {
        _iconNode = new IconImageNode
        {
            Size = new Vector2(48, 64),
            IsVisible = true,
            EnableEventFlags = true,
        };
        Services.NativeController.AttachNode(_iconNode, this); 
        
        _progressNode = new ProgressBarNode
        {
            Progress = 1f,
            IsVisible = true,
            Width = 200,
        };
        Services.NativeController.AttachNode(_progressNode, this);
        
        _statusName = new TextNode {
            IsVisible = true,
            FontSize = 20,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge,
        };
        
        Services.NativeController.AttachNode(_statusName, this);
        
        _statusRemaining = new TextNode {
            IsVisible = true,
            FontSize = 20,
            Width = 60,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge,
            AlignmentType = AlignmentType.TopRight,
        };
        Services.NativeController.AttachNode(_statusRemaining, this);
        
        _iconNode.AddEvent(AddonEventType.MouseClick, (e) => StatusNodeClick(this, e));
        
        Height = 50;
        Width = 300;
    }

    public StatusInfo StatusInfo
    {
        get => _statusInfo;
        set
        {
            _statusInfo = value;
            UpdateValues();
        }
    }

    public void UpdateValues()
    {
        _statusName.Text = _statusInfo.Name;
        _iconNode.IconId = _statusInfo.IconId;
        
        if (_statusInfo.IsPermanent)
        {
            _progressNode.IsVisible = false;
            _statusRemaining.IsVisible = !_statusInfo.IsPermanent;
            return;
        }

        // Set max to 1f because we sometimes get insane values just as the buff is added
        // Clamp/Lerp the rest because after 0.06f the bar is already visually empty
        float max = Math.Max(_statusInfo.MaxSeconds, 1f);
        float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
        float ratio = remaining / max;
        
        _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
        _statusRemaining.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
    }
    
    private static unsafe void StatusNodeClick(StatusTimerNode node, AddonEventData eventData)
    {
        var atkEventData = (AtkEventData*) eventData.AtkEventDataPointer;
        if (atkEventData->MouseData.ButtonId == 1)
            StatusManager.ExecuteStatusOff(node.StatusInfo.Id);
    }

    // Override Width property to have it set the width of our individual parts
    public override float Width {
        get => base.Width;
        set {
            base.Width = value;
            
            _statusName.Width = value - _iconNode.Width - _statusRemaining.Width - 10;
            _statusName.X = _iconNode.Width + 5;
            _progressNode.X = _iconNode.Width + 2;
            _statusRemaining.X = value - _statusRemaining.Width - 5;
        }
    }

    // Override Height property to have it set the height of our individual parts
    public override float Height {
        get => base.Height;
        set {
            base.Height = value;
            
            _iconNode.Y = (value - _iconNode.Height) / 2;
            _progressNode.Y = value -  _progressNode.Height;
            _statusName.Y = _statusName.Height / 2;
            _statusRemaining.Y = _statusRemaining.Height / 2;
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