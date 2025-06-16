using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using KamiToolKit;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;

namespace StatusTimers.Windows;

public class AddonStatusTimers : NativeAddon {
    private const float UnitSize = 65.0f;
    private const float FramePadding = 8.0f;
    private const float UnitPadding = 10.0f;
    private const float VerticalPadding = UnitPadding / 4.0f;
    
    private readonly Dictionary<uint, StatusTimerNode> _statusTimers = new();
    private readonly List<StatusTimerNode> _activeTimers = new();
    private readonly Stack<StatusTimerNode> _recycledTimers = new();
    
    // OnSetup is your entry-point to adding native elements to the window
    // Here you should allocate and attach your nodes to the UI
    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        addon->Alpha = 0;
        var xPos = FramePadding;
        var yPos = Size.Y - UnitSize - FramePadding;

        // Attach custom node to addon
        //
        // IMPORTANT: Once attached, >> do not detach or dispose these nodes <<
        // When attaching the game will take ownership of the nodes and all associated data,
        // and will properly clean up when the addon is closed

        /*
        TextNode statusNode = new TextNode
        {
            Position = new Vector2(xPos, yPos),
            Size = new Vector2(UnitSize, UnitSize),
            IsVisible = true,
            Text = "Test",
        };
        
        
        NativeController.AttachNode(statusNode, this);

        xPos += statusNode.Width + UnitPadding;
        yPos += statusNode.Height + UnitPadding;
        */
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon)
    {
        var statuses = StatusManager.GetPlayerStatuses();

        // Step 1: Mark all current active timers for recycle
        foreach (var node in _activeTimers)
            node.IsVisible = false;

        foreach (var node in _activeTimers)
            _recycledTimers.Push(node);
        _activeTimers.Clear();

        // Step 2: Reuse or allocate new nodes
        foreach (var status in statuses)
        {
            StatusTimerNode node;

            if (_recycledTimers.Count > 0)
            {
                node = _recycledTimers.Pop();
            }
            else
            {
                node = new StatusTimerNode();
                NativeController.AttachNode(node, this); // 🔹 Only once per node lifetime
            }

            node.Name = status.Name;
            node.StatusIconId = status.IconId;
            node.TimeRemaining = status.RemainingSeconds;
            node.IsVisible = true;

            _activeTimers.Add(node);
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
                Position = new Vector2(50, 5),
                FontSize = 20,
                Width = 180,
            };
            Services.NativeController.AttachNode(_statusName, this);
            
            _statusRemaining = new TextNode {
                IsVisible = true,
                Position = new Vector2(240, 5),
                FontSize = 20,
                Width = 60,
                AlignmentType = AlignmentType.Right,
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
                _statusName.Width = value - 48.0f;
                _statusName.X = 24.0f;
            }
        }

        // Override Height property to have it set the height of our individual parts
        public override float Height {
            get => base.Height;
            set {
                base.Height = value;
                _statusName.Height = value - 48.0f;
                _statusName.Y = 24.0f;
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