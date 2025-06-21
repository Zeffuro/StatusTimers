using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Helpers;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

public abstract class StatusTimerWindow<TKey> : NativeAddon {
    private const int PoolSize = 30;

    private const float FramePadding = 8.0f;
    private const float UnitPadding = 10.0f;
    private const float VerticalPadding = UnitPadding / 4.0f;
    private readonly NodeKind _nodeKind;

    private readonly IStatusSource<TKey> _source;

    protected readonly Dictionary<TKey, StatusTimerNode> Active = new();
    protected readonly Stack<StatusTimerNode> Pool = new();

    private NodeBase _rootContainer;
    private readonly List<StatusTimerNode> _allNodes = new();
    private List<HorizontalListNode<StatusTimerNode>> _rows = new();
    private List<VerticalListNode<StatusTimerNode>> _columns = new();

    private GrowDirection _growDirection;

    private TextNode _headerNode;

    private void SetupContainers<TOuter, TInner>(
        Func<TOuter> createOuter,
        Action<TOuter> attachOuter,
        Func<TInner> createInner,
        Action<TOuter, TInner> addInnerToOuter,
        Action<TInner> configureInner,
        Action<TInner, StatusTimerNode> addNodeToInner,
        int outerCount,
        int itemsPerInner
    )
        where TOuter : NodeBase
        where TInner : NodeBase
    {
        _allNodes.Clear();

        var outer = createOuter();

        for (int i = 0, nodeIndex = 0; i < outerCount && nodeIndex < PoolSize; i++) {
            var inner = createInner();
            configureInner(inner);
            addInnerToOuter(outer, inner);

            for (int j = 0; j < itemsPerInner && nodeIndex < PoolSize; j++, nodeIndex++) {
                var node = new StatusTimerNode { IsVisible = false };
                addNodeToInner(inner, node);
                _allNodes.Add(node);
            }
        }

        // Attach only the outermost container once
        attachOuter(outer);

        _rootContainer = outer;
    }

    protected StatusTimerWindow(IStatusSource<TKey> source, NodeKind nodeKind) {
        _source = source;
        _nodeKind = nodeKind;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        WindowNode.IsVisible = false;

        _headerNode = new TextNode {
            IsVisible = true,
            X = FramePadding * 2 + 44,
            Width = 300,
            Height = 26,
            FontSize = 24,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge,
            Text = Title
        };
        NativeController.AttachNode(_headerNode, this);

        int outerCount = (int)Math.Ceiling(PoolSize / (double)ItemsPerLine);

        if (FillRowsFirst) {
            _rows.Clear();

            SetupContainers(
                createOuter: () => new VerticalListNode<HorizontalListNode<StatusTimerNode>> {
                    Width = 600,
                    Height = 500,
                    IsVisible = true,
                    ItemVerticalSpacing = 2
                },
                attachOuter: outer => NativeController.AttachNode(outer, this),
                createInner: () => {
                    var horizontalListlist = new HorizontalListNode<StatusTimerNode> {
                        Width = 600,
                        Height = 50,
                        IsVisible = true,
                        ItemHorizontalSpacing = 5
                    };
                    _rows.Add(horizontalListlist);
                    return horizontalListlist;
                },
                addInnerToOuter: (outer, inner) => outer.Add(inner),
                configureInner: inner => { },
                addNodeToInner: (inner, node) => inner.Add(node),
                outerCount: outerCount,
                itemsPerInner: ItemsPerLine
            );
        }
        else {
            _columns.Clear();

            SetupContainers(
                createOuter: () => new HorizontalListNode<VerticalListNode<StatusTimerNode>> {
                    Width = 600,
                    Height = 500,
                    IsVisible = true,
                    ItemHorizontalSpacing = 5
                },
                attachOuter: outer => NativeController.AttachNode(outer, this),
                createInner: () => {
                    var verticalList = new VerticalListNode<StatusTimerNode> {
                        Height = 500,
                        Width = 300,
                        IsVisible = true,
                        ItemVerticalSpacing = 2,
                        Alignment = VerticalListAnchor.Top
                    };
                    _columns.Add(verticalList);
                    return verticalList;
                },
                addInnerToOuter: (outer, inner) => outer.Add(inner),
                configureInner: inner => { },
                addNodeToInner: (inner, node) => inner.Add(node),
                outerCount: outerCount,
                itemsPerInner: ItemsPerLine
            );
        }

        ApplyGrowDirection();
    }


    public bool FillRowsFirst { get; set; } = false;

    public int ItemsPerLine { get; set; } = 2;

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        IReadOnlyList<StatusInfo> current = _source.Fetch();

        var sortedStatuses = current
            .OrderByDescending(s => s.IsPermanent)
            .ThenByDescending(s => s.RemainingSeconds)
            .ToList();

        int i = 0;
        var newActive = new Dictionary<TKey, StatusTimerNode>();

        for (; i < sortedStatuses.Count && i < _allNodes.Count; i++) {
            var status = sortedStatuses[i];
            var node = _allNodes[i];

            newActive[_source.KeyOf(status)] = node;

            node.StatusInfo = status;
            node.Kind = _nodeKind;
            node.IsVisible = true;
        }

        for (; i < _allNodes.Count; i++) {
            _allNodes[i].IsVisible = false;
        }

        bool alignRight = _growDirection == GrowDirection.DownLeft || _growDirection == GrowDirection.UpLeft;

        foreach (var node in _allNodes) {
            if (!node.IsVisible) {
                continue;
            }

            node.X = alignRight ? (_rootContainer.Width - node.Width) : 0;
        }

        Active.Clear();
        foreach (var kv in newActive) {
            Active[kv.Key] = kv.Value;
        }
    }

    private void ApplyGrowDirection() {
        switch (_growDirection) {
            case GrowDirection.DownRight:
            case GrowDirection.DownLeft:
                SetVerticalAlignment(VerticalListAnchor.Top);
                break;
            case GrowDirection.UpRight:
            case GrowDirection.UpLeft:
                SetVerticalAlignment(VerticalListAnchor.Bottom);
                break;
        }
    }

    private void SetVerticalAlignment(VerticalListAnchor anchor) {
        if (_rootContainer is VerticalListNode<HorizontalListNode<StatusTimerNode>> verticalRoot) {
            verticalRoot.Alignment = anchor;
        }
        else if (_rootContainer is HorizontalListNode<VerticalListNode<StatusTimerNode>>) {
            foreach (var verticalList in _columns) {
                verticalList.Alignment = anchor;
            }
        }
    }

    public enum GrowDirection {
        DownRight,
        DownLeft,
        UpRight,
        UpLeft
    }
}
