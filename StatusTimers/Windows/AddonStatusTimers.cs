using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using KamiToolKit;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using CSStatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager; 

namespace StatusTimers.Windows;

public class AddonStatusTimers : NativeAddon {
    private const float UnitSize = 65.0f;
    private const float FramePadding = 8.0f;
    private const float UnitPadding = 10.0f;
    private const float VerticalPadding = UnitPadding / 4.0f;
    private const float MaxAmountOfNodes = 100;
    
    private readonly Dictionary<uint, StatusTimerNode> _statusTimers = new();
    private readonly List<StatusTimerNode> _activeTimers = new();
    private readonly Stack<StatusTimerNode> _recycledTimers = new();
    
    private CSStatusManager  _csStatusManager = new CSStatusManager();
    
    // OnSetup is your entry-point to adding native elements to the window
    // Here you should allocate and attach your nodes to the UI
    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        // TODO Determine max amount of nodes CSStatusManager.NumValidStatuses seems to give 0 for some reason
        this.WindowNode.IsVisible = false;
        
        for (int i = 0; i < MaxAmountOfNodes; i++)
        {
            var node = new StatusTimerNode();
            NativeController.AttachNode(node, this);
            node.IsVisible = false;
            _statusTimers[(uint)i] = node;
            _recycledTimers.Push(node);
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon)
    {
        var statuses = StatusManager.GetPlayerStatuses();

        foreach (var node in _activeTimers)
            node.IsVisible = false;

        foreach (var node in _activeTimers)
            _recycledTimers.Push(node);
        _activeTimers.Clear();

        foreach (var status in statuses)
        {
            if (_recycledTimers.Count == 0)
                break; // Prevent overuse of pool

            var node = _recycledTimers.Pop();

            node.StatusInfo = status;
            node.IsVisible = true;

            _activeTimers.Add(node);
        }

        LayoutNodes();
    }

    private static unsafe void StatusNodeClick(StatusTimerNode node, AddonEventData eventData)
    {
        var atkEventData = (AtkEventData*) eventData.AtkEventDataPointer;
        if (atkEventData->MouseData.ButtonId == 1)
            CSStatusManager.ExecuteStatusOff(node.StatusInfo.Id);
    }
    
    private void LayoutNodes()
    {
        float x = FramePadding;
        float y = FramePadding;

        foreach (var node in _activeTimers)
        {
            node.Position = new Vector2(x, y);
            y += node.Height + VerticalPadding;
        }
    }

    // OnHide is called when our window is about to close, but hasn't closed yet.
    // If you need an event to trigger immediately before the window actually closes, use OnFinalize
    protected override unsafe void OnHide(AtkUnitBase* addon) {
        
    }
    
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
            _progressNode.IsVisible = !_statusInfo.IsPermanent;
            
            // Set max to 1f because we sometimes get insane values just as the buff is added
            // Clamp/Lerp the rest because after 0.06f the bar is already visually empty
            float max = Math.Max(_statusInfo.MaxSeconds, 1f);
            float remaining = Math.Clamp(_statusInfo.RemainingSeconds, 0f, max);
            float ratio = remaining / max;
            
            _progressNode.Progress = 0.06f + (1f - 0.06f) * ratio;
            _statusName.Text = _statusInfo.Name;
            _statusRemaining.IsVisible = !_statusInfo.IsPermanent;
            _statusRemaining.Text = $"{_statusInfo.RemainingSeconds:0.0}s";
            _iconNode.IconId = _statusInfo.IconId;
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
}