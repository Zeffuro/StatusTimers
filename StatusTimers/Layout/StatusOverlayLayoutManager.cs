using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Layout;

public class StatusOverlayLayoutManager<TKey> : IDisposable {
    private bool isDisposed = false;

    private readonly List<StatusTimerNode<TKey>> _allNodes = new();
    private readonly List<VerticalListNode<StatusTimerNode<TKey>>> _columns = new();
    private readonly Func<StatusTimerOverlayConfig?> _getOverlayConfig;

    private readonly StatusTimerOverlay<TKey> _ownerOverlay;
    private readonly List<HorizontalListNode<StatusTimerNode<TKey>>> _rows = new();

    private NineGridNode _backgroundNode;

    private StatusTimerNode<TKey>.StatusNodeActionHandler _onNodeActionTriggered;
    private NodeBase _rootContainer;

    public StatusOverlayLayoutManager(
        StatusTimerOverlay<TKey> ownerOverlay,
        Func<StatusTimerOverlayConfig?> getOverlayConfig) {
        _ownerOverlay = ownerOverlay;
        _getOverlayConfig = getOverlayConfig;
    }

    public Vector2 CalculatedOverlaySize { get; private set; }

    public NodeBase RootContainer => _rootContainer;
    public NineGridNode BackgroundNode => _backgroundNode;

    public IReadOnlyList<StatusTimerNode<TKey>> AllNodes => _allNodes;

    public void SetNodeActionHandler(StatusTimerNode<TKey>.StatusNodeActionHandler handler) {
        _onNodeActionTriggered = handler;
    }

    public void InitializeLayout() {
        CalculatedOverlaySize = CalculateOverlaySize();

        _backgroundNode = new NineGridNode {
            NodeId = 2,
            Size = CalculatedOverlaySize,
            BottomOffset = 8,
            TopOffset = 21,
            LeftOffset = 21,
            RightOffset = 21
        };
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(44, 32),
            TextureCoordinates = new Vector2(0, 0)
        });
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(88, 16),
            TextureCoordinates = new Vector2(0, 16)
        });
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(156, 80),
            TextureCoordinates = new Vector2(0, 24)
        });

        GlobalServices.NativeController.AttachNode(_backgroundNode, _ownerOverlay);

        BuildContainers();
    }

    public Vector2 CalculateOverlaySize() {
        var config = _getOverlayConfig();

        float totalWidth;
        float totalHeight;

        int maxItems = config.MaxStatuses;
        int itemsPerLine = Math.Min(config.ItemsPerLine, maxItems);

        if (config.FillRowsFirst) {
            int numRows = (int)Math.Ceiling(maxItems / (double)itemsPerLine);

            int itemsInLastRow = maxItems % itemsPerLine;
            if (itemsInLastRow == 0 && maxItems > 0) {
                itemsInLastRow = itemsPerLine;
            }

            float widestRowWidth = Math.Max(
                itemsPerLine * config.RowWidth + (itemsPerLine - 1) * config.StatusHorizontalPadding,
                itemsInLastRow * config.RowWidth + Math.Max(0, (itemsInLastRow - 1)) * config.StatusHorizontalPadding
            );

            float allRowsHeight = numRows * config.RowHeight +
                                  (numRows - 1) * config.StatusVerticalPadding;

            totalWidth = widestRowWidth;
            totalHeight = allRowsHeight;
        }
        else {
            int numCols = (int)Math.Ceiling(maxItems / (double)itemsPerLine);

            int itemsInLastCol = maxItems % itemsPerLine;
            if (itemsInLastCol == 0 && maxItems > 0) {
                itemsInLastCol = itemsPerLine;
            }

            float tallestColHeight = Math.Max(
                itemsPerLine * config.RowHeight + (itemsPerLine - 1) * config.StatusVerticalPadding,
                itemsInLastCol * config.RowHeight + Math.Max(0, (itemsInLastCol - 1)) * config.StatusVerticalPadding
            );

            float allColsWidth = numCols * config.RowWidth +
                                 (numCols - 1) * config.StatusHorizontalPadding;

            totalWidth = allColsWidth;
            totalHeight = tallestColHeight;
        }

        return new Vector2(Math.Max(0, totalWidth), Math.Max(0, totalHeight));
    }

    private void BuildContainersOnly() {
        CalculatedOverlaySize = CalculateOverlaySize();

        _backgroundNode = new NineGridNode {
            Size = CalculatedOverlaySize,
            BottomOffset = 8,
            TopOffset = 21,
            LeftOffset = 21,
            RightOffset = 21
        };
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(44, 32),
            TextureCoordinates = new Vector2(0, 0)
        });
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(88, 16),
            TextureCoordinates = new Vector2(0, 16)
        });
        _backgroundNode.AddPart(new Part {
            TexturePath = "ui/uld/HUDLayout.tex",
            Size = new Vector2(156, 80),
            TextureCoordinates = new Vector2(0, 24)
        });

        GlobalServices.NativeController.AttachNode(_backgroundNode, _ownerOverlay);

        BuildContainers();
    }

    public void BuildContainers() {
        _allNodes.Clear();
        _rows.Clear();
        _columns.Clear();

        var config = _getOverlayConfig();

        CalculatedOverlaySize = CalculateOverlaySize();
        int outerCount = (int)Math.Ceiling(config.MaxStatuses / (double)config.ItemsPerLine);

        if (config.FillRowsFirst) {
            SetupContainers(
                () => new VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> {
                    NodeId = 3,
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemSpacing = config.StatusVerticalPadding
                },
                outer => GlobalServices.NativeController.AttachNode(outer, _ownerOverlay),
                () => {
                    float innerWidth = Math.Min(config.ItemsPerLine, config.MaxStatuses) * config.RowWidth +
                                       (Math.Min(config.ItemsPerLine, config.MaxStatuses) - 1) *
                                       config.StatusHorizontalPadding;
                    float innerHeight = config.RowHeight;

                    HorizontalListNode<StatusTimerNode<TKey>> list = new() {
                        NodeId = 2,
                        Width = innerWidth,
                        Height = innerHeight,
                        IsVisible = true,
                        ItemSpacing = config.StatusHorizontalPadding
                    };
                    _rows.Add(list);
                    return list;
                },
                (outer, inner) => outer.AddNode(inner),
                inner => { },
                (inner, node) => {
                    inner.AddNode(node);
                    node.OuterContainer = inner;
                },
                outerCount,
                config.ItemsPerLine
            );
        }
        else {
            SetupContainers(
                () => new HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> {
                    NodeId = 3,
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemSpacing = config.StatusHorizontalPadding
                },
                outer => GlobalServices.NativeController.AttachNode(outer, _ownerOverlay),
                () => {
                    float innerWidth = config.RowWidth;
                    float innerHeight = Math.Min(config.ItemsPerLine, config.MaxStatuses) * config.RowHeight +
                                        (Math.Min(config.ItemsPerLine, config.MaxStatuses) - 1) *
                                        config.StatusVerticalPadding;
                    VerticalListNode<StatusTimerNode<TKey>> list = new() {
                        NodeId = 2,
                        Height = innerHeight,
                        Width = innerWidth,
                        IsVisible = true,
                        ItemSpacing = config.StatusVerticalPadding,
                        Alignment = VerticalListAnchor.Top
                    };
                    _columns.Add(list);
                    return list;
                },
                (outer, inner) => outer.AddNode(inner),
                inner => { },
                (inner, node) => {
                    inner.AddNode(node);
                    node.OuterContainer = inner;
                },
                outerCount,
                config.ItemsPerLine
            );
        }

        _ownerOverlay.Size = CalculatedOverlaySize;
        ToggleBackground(_ownerOverlay.IsLocked);
    }

    private bool _isRebuilding = false;

    public void RebuildContainers(Action onCompleteCallback = null) {
        if (_rootContainer != null) {
            GlobalServices.NativeController.DetachNode(_rootContainer);
            _rootContainer = null;
        }
        if (_backgroundNode != null) {
            GlobalServices.NativeController.DetachNode(_backgroundNode);
            _backgroundNode = null;
        }
        _allNodes.Clear();
        _rows.Clear();
        _columns.Clear();

        BuildContainersOnly();

        GlobalServices.Framework.RunOnTick(() => {
            if (isDisposed) {
                return;
            }

            try {
                RecalculateLayout();
                onCompleteCallback?.Invoke();
            }
            finally {
                _isRebuilding = false;
            }
        }, delayTicks: 3);
    }

    public void RecalculateLayout()
    {
        var config = _getOverlayConfig();
        var grow = config.GrowDirection;

        VerticalListAnchor verticalAnchor;
        HorizontalListAnchor horizontalAnchor;

        switch (grow)
        {
            case GrowDirection.UpLeft:
                verticalAnchor = VerticalListAnchor.Bottom;
                horizontalAnchor = HorizontalListAnchor.Right;
                break;
            case GrowDirection.UpRight:
                verticalAnchor = VerticalListAnchor.Bottom;
                horizontalAnchor = HorizontalListAnchor.Left;
                break;
            case GrowDirection.DownLeft:
                verticalAnchor = VerticalListAnchor.Top;
                horizontalAnchor = HorizontalListAnchor.Right;
                break;
            case GrowDirection.DownRight:
            default:
                verticalAnchor = VerticalListAnchor.Top;
                horizontalAnchor = HorizontalListAnchor.Left;
                break;
        }

        SetVerticalAlignment(verticalAnchor);
        SetHorizontalAlignment(horizontalAnchor);

        float rootContainerOffsetX = 0, rootContainerOffsetY = 0;

        if (_rootContainer != null)
        {
            _rootContainer.X = rootContainerOffsetX;
            _rootContainer.Y = rootContainerOffsetY;

            switch (_rootContainer)
            {
                case VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> vRoot:
                    vRoot.RecalculateLayout();
                    break;
                case HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> hRoot:
                    hRoot.RecalculateLayout();
                    break;
            }
        }
    }

    public void ToggleBackground(bool isLocked) {
        if (_backgroundNode != null) {
            _backgroundNode.IsVisible = !isLocked;
        }
    }

    public void UpdateAllNodesDisplay() {
        foreach (StatusTimerNode<TKey> node in _allNodes) {
            node.ApplyOverlayConfig();
        }
    }

    public void UpdateNodeContent(List<StatusInfo> finalSortedList, NodeKind nodeKind) {
        var config = _getOverlayConfig();

        int i = 0;
        for (; i < finalSortedList.Count && i < _allNodes.Count; i++) {
            StatusInfo status = finalSortedList[i];
            StatusTimerNode<TKey> node = _allNodes[i];

            node.StatusInfo = status;
            node.Kind = nodeKind;
            if (!node.IsVisible) {
                node.IsVisible = true;
            }

            node.UpdateValues();
        }

        for (; i < _allNodes.Count; i++) {
            _allNodes[i].IsVisible = false;
        }
    }

    private void SetupContainers<TOuter, TInner>(
        Func<TOuter> createOuter,
        Action<TOuter> attachOuter,
        Func<TInner> createInner,
        Action<TOuter, TInner> addInnerToOuter,
        Action<TInner> configureInner,
        Action<TInner, StatusTimerNode<TKey>> addNodeToInner,
        int outerCount,
        int itemsPerInner
    )
        where TOuter : NodeBase
        where TInner : NodeBase {
        TOuter outer = createOuter();

        for (int i = 0, nodeIndex = 0; i < outerCount && nodeIndex < _getOverlayConfig().MaxStatuses; i++) {
            TInner inner = createInner();
            configureInner(inner);
            addInnerToOuter(outer, inner);

            for (int j = 0; j < itemsPerInner && nodeIndex < _getOverlayConfig().MaxStatuses; j++, nodeIndex++) {
                StatusTimerNode<TKey> node = new(_getOverlayConfig) {
                    Height = _getOverlayConfig().RowHeight,
                    Width = _getOverlayConfig().RowWidth,
                    Origin = new Vector2(_getOverlayConfig().RowWidth / 2, _getOverlayConfig().RowHeight / 2),
                    IsVisible = false
                };
                addNodeToInner(inner, node);
                _allNodes.Add(node);

                if (_onNodeActionTriggered != null) {
                    node.OnStatusNodeActionTriggered += _onNodeActionTriggered;
                }
            }
        }

        attachOuter(outer);
        _rootContainer = outer;
    }

    private void SetVerticalAlignment(VerticalListAnchor anchor) {
        if (_rootContainer is VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> verticalRoot) {
            verticalRoot.Alignment = anchor;
            verticalRoot.RecalculateLayout();
        }
        else if (_rootContainer is HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>>) {
            foreach (VerticalListNode<StatusTimerNode<TKey>> verticalList in _columns) {
                verticalList.Alignment = anchor;
                verticalList.RecalculateLayout();
            }
        }
    }

    private void SetHorizontalAlignment(HorizontalListAnchor anchor) {
        if (_rootContainer is VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>>) {
            foreach (HorizontalListNode<StatusTimerNode<TKey>> horizontalList in _rows) {
                horizontalList.Alignment = anchor;
                horizontalList.RecalculateLayout();
            }
        }
        else if (_rootContainer is HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> horizontalRoot) {
            horizontalRoot.Alignment = anchor;
            horizontalRoot.RecalculateLayout();
        }
    }

    public void SetNodeNull(bool isBackground) {
        if (isBackground) {
            _backgroundNode = null;
        }
        else {
            _rootContainer = null;
        }
    }

    public void UnsubscribeFromNodeActions() {
        if (_onNodeActionTriggered != null) {
            foreach (StatusTimerNode<TKey> node in _allNodes) {
                node.OnStatusNodeActionTriggered -= _onNodeActionTriggered;
            }
        }
    }
    public void Dispose() {
        isDisposed = true;
    }
}
