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
        this.WindowNode.IsVisible = true;
        
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

            node.Name = status.Name;
            node.StatusIconId = status.IconId;
            node.TimeRemaining = status.RemainingSeconds;
            node.IsVisible = true;

            _activeTimers.Add(node);

            if (status.Id == 1199)
            {
                //CSStatusManager.ExecuteStatusOff(1199);
            }
        }

        LayoutNodes();
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
        private readonly IconImageNode _iconNode;

        public StatusTimerNode() {
            
            _iconNode = new IconImageNode
            {
                Size = new Vector2(48, 64),
                IsVisible = true,
            };
            Services.NativeController.AttachNode(_iconNode, this); 
            
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
            
            Height = 50;
            Width = 300;
        }

        public string Name {
            get => _statusName.Text.ToString();
            set => _statusName.Text = value;
        }

        public uint StatusIconId
        {
            get => _iconNode.IconId;
            set => _iconNode.IconId = value;
        }
        
        public float TimeRemaining { 
            get => float.Parse(_statusRemaining.Text.ToString());
            set => _statusRemaining.Text = $"{value:0.0}s"; 
        }

        // Override Width property to have it set the width of our individual parts
        public override float Width {
            get => base.Width;
            set {
                base.Width = value;
                
                _statusName.Width = value - _iconNode.Width - _statusRemaining.Width - 10;
                _statusName.X = _iconNode.Width + 5;
                _statusRemaining.X = value - _statusRemaining.Width - 5;
            }
        }

        // Override Height property to have it set the height of our individual parts
        public override float Height {
            get => base.Height;
            set {
                base.Height = value;
                
                _iconNode.Y = (value - _iconNode.Height) / 2;
                _statusName.Y = (value - _statusName.Height) / 2;
                _statusRemaining.Y = (value - _statusRemaining.Height) / 2;
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