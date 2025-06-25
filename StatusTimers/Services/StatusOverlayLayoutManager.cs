using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Services;

public class StatusOverlayLayoutManager<TKey> {
    private const float StatusNodeWidth = 300;
    private const float StatusNodeHeight = 60;

    private readonly List<StatusTimerNode<TKey>> _allNodes = new();
    private readonly List<VerticalListNode<StatusTimerNode<TKey>>> _columns = new();
    private readonly Func<bool> _getAllowDismissStatus;
    private readonly Func<bool> _getAllowTargetActor;
    private readonly Func<bool> _getAnimationsEnabled;
    private readonly Func<bool> _getFillRowsFirst;
    private readonly Func<GrowDirection> _getGrowDirection;
    private readonly Func<bool> _getIsLocked;
    private readonly Func<int> _getItemsPerLine;
    private readonly Func<int> _getMaxStatuses;
    private readonly Func<bool> _getShowActorLetter;
    private readonly Func<bool> _getShowActorName;
    private readonly Func<bool> _getShowIcon;
    private readonly Func<bool> _getShowProgress;
    private readonly Func<bool> _getShowStatusName;
    private readonly Func<bool> _getShowStatusRemaining;
    private readonly Func<bool> _getShowStatusRemainingBackground;
    private readonly Func<int> _getStatusHorizontalPadding;
    private readonly Func<TextStyle> _getStatusRemainingTextStyle;
    private readonly Func<int> _getStatusVerticalPadding;

    private readonly NodeKind _nodeKind;

    private readonly SimpleComponentNode _ownerOverlay;
    private readonly List<HorizontalListNode<StatusTimerNode<TKey>>> _rows = new();

    private NineGridNode _backgroundNode;

    private StatusTimerNode<TKey>.StatusNodeActionHandler _onNodeActionTriggered;
    private NodeBase _rootContainer;

    public StatusOverlayLayoutManager(
        SimpleComponentNode ownerOverlay,
        NodeKind nodeKind,
        Func<int> getMaxStatuses,
        Func<int> getItemsPerLine,
        Func<int> getStatusHorizontalPadding,
        Func<int> getStatusVerticalPadding,
        Func<GrowDirection> getGrowDirection,
        Func<bool> getIsLocked,
        Func<TextStyle> getStatusRemainingTextStyle,
        Func<bool> getShowIcon,
        Func<bool> getShowStatusName,
        Func<bool> getShowStatusRemaining,
        Func<bool> getShowProgress,
        Func<bool> getShowStatusRemainingBackground,
        Func<bool> getShowActorLetter,
        Func<bool> getShowActorName,
        Func<bool> getAllowDismissStatus,
        Func<bool> getAllowTargetActor, // Corrected type, assuming this is from your StatusTimerOverlay.cs
        Func<bool> getAnimationsEnabled,
        Func<bool> getFillRowsFirst) {
        _ownerOverlay = ownerOverlay;
        _nodeKind = nodeKind;
        _getMaxStatuses = getMaxStatuses;
        _getItemsPerLine = getItemsPerLine;
        _getStatusHorizontalPadding = getStatusHorizontalPadding;
        _getStatusVerticalPadding = getStatusVerticalPadding;
        _getGrowDirection = getGrowDirection;
        _getIsLocked = getIsLocked;
        _getStatusRemainingTextStyle = getStatusRemainingTextStyle;
        _getShowIcon = getShowIcon;
        _getShowStatusName = getShowStatusName;
        _getShowStatusRemaining = getShowStatusRemaining;
        _getShowProgress = getShowProgress;
        _getShowStatusRemainingBackground = getShowStatusRemainingBackground;
        _getShowActorLetter = getShowActorLetter;
        _getShowActorName = getShowActorName;
        _getAllowDismissStatus = getAllowDismissStatus;
        _getAllowTargetActor = getAllowTargetActor;
        _getAnimationsEnabled = getAnimationsEnabled;
        _getFillRowsFirst = getFillRowsFirst;
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
        float totalWidth = 0;
        float totalHeight = 0;

        int maxItems = _getMaxStatuses();
        int itemsPerLine = Math.Min(_getItemsPerLine(), maxItems);

        if (_getFillRowsFirst()) {
            int numRows = (int)Math.Ceiling(maxItems / (double)itemsPerLine);

            int itemsInLastRow = maxItems % itemsPerLine;
            if (itemsInLastRow == 0 && maxItems > 0) {
                itemsInLastRow = itemsPerLine;
            }

            float widestRowWidth = Math.Max(
                itemsPerLine * StatusNodeWidth + (itemsPerLine - 1) * _getStatusHorizontalPadding(),
                itemsInLastRow * StatusNodeWidth + Math.Max(0, (itemsInLastRow - 1)) * _getStatusHorizontalPadding()
            );

            float allRowsHeight = numRows * StatusNodeHeight +
                                  (numRows - 1) * _getStatusVerticalPadding();

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
                itemsPerLine * StatusNodeHeight + (itemsPerLine - 1) * _getStatusVerticalPadding(),
                itemsInLastCol * StatusNodeHeight + Math.Max(0, (itemsInLastCol - 1)) * _getStatusVerticalPadding()
            );

            float allColsWidth = numCols * StatusNodeWidth +
                                 (numCols - 1) * _getStatusHorizontalPadding();

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

        CalculatedOverlaySize = CalculateOverlaySize();
        int outerCount = (int)Math.Ceiling(_getMaxStatuses() / (double)_getItemsPerLine());
        StatusNodeDisplayConfig initialNodeConfig = GetCurrentStatusNodeDisplayConfig();

        if (_getFillRowsFirst()) {
            SetupContainers(
                () => new VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemVerticalSpacing = _getStatusVerticalPadding()
                },
                outer => GlobalServices.NativeController.AttachNode(outer, _ownerOverlay),
                () => {
                    float innerWidth = Math.Min(_getItemsPerLine(), _getMaxStatuses()) * StatusNodeWidth +
                                       (Math.Min(_getItemsPerLine(), _getMaxStatuses()) - 1) *
                                       _getStatusHorizontalPadding();
                    float innerHeight = StatusNodeHeight;

                    HorizontalListNode<StatusTimerNode<TKey>> list = new() {
                        Width = innerWidth,
                        Height = innerHeight,
                        IsVisible = true,
                        ItemHorizontalSpacing = _getStatusHorizontalPadding()
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
                _getItemsPerLine(),
                initialNodeConfig
            );
        }
        else {
            SetupContainers(
                () => new HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemHorizontalSpacing = _getStatusHorizontalPadding()
                },
                outer => GlobalServices.NativeController.AttachNode(outer, _ownerOverlay),
                () => {
                    float innerWidth = StatusNodeWidth;
                    float innerHeight = Math.Min(_getItemsPerLine(), _getMaxStatuses()) * StatusNodeHeight +
                                        (Math.Min(_getItemsPerLine(), _getMaxStatuses()) - 1) *
                                        _getStatusVerticalPadding();
                    VerticalListNode<StatusTimerNode<TKey>> list = new() {
                        Height = innerHeight,
                        Width = innerWidth,
                        IsVisible = true,
                        ItemVerticalSpacing = _getStatusVerticalPadding(),
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
                _getItemsPerLine(),
                initialNodeConfig
            );
        }

        _ownerOverlay.Size = CalculatedOverlaySize;
        ToggleDrag(_getIsLocked());
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
        var grow = _getGrowDirection();

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
        if (verticalAnchor == VerticalListAnchor.Bottom) {
            rootContainerOffsetY = -StatusNodeHeight;
        }

        if (horizontalAnchor == HorizontalListAnchor.Right) {
            rootContainerOffsetX = -StatusNodeWidth;
        }

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

    public void ToggleDrag(bool isLocked) {
        if (_backgroundNode != null) {
            _backgroundNode.IsVisible = !isLocked;
        }

        foreach (StatusTimerNode<TKey> node in _allNodes) {
            node.ToggleEventFlags(isLocked);
        }
    }

    public void UpdateAllNodesDisplay() {
        StatusNodeDisplayConfig currentConfig = GetCurrentStatusNodeDisplayConfig();
        foreach (StatusTimerNode<TKey> node in _allNodes) {
            node.ApplyDisplayConfig(currentConfig);
        }
    }

    public void UpdateNodeContent(List<StatusInfo> finalSortedList, NodeKind nodeKind) {
        StatusNodeDisplayConfig currentDisplayConfig = GetCurrentStatusNodeDisplayConfig();

        int i = 0;
        for (; i < finalSortedList.Count && i < _allNodes.Count; i++) {
            StatusInfo status = finalSortedList[i];
            StatusTimerNode<TKey> node = _allNodes[i];

            node.StatusInfo = status;
            node.Kind = nodeKind;
            if (!node.IsVisible) {
                node.IsVisible = true;
            }

            node.UpdateValues(currentDisplayConfig);
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
        int itemsPerInner,
        StatusNodeDisplayConfig initialNodeConfig
    )
        where TOuter : NodeBase
        where TInner : NodeBase {
        TOuter outer = createOuter();

        for (int i = 0, nodeIndex = 0; i < outerCount && nodeIndex < _getMaxStatuses(); i++) {
            TInner inner = createInner();
            configureInner(inner);
            addInnerToOuter(outer, inner);

            for (int j = 0; j < itemsPerInner && nodeIndex < _getMaxStatuses(); j++, nodeIndex++) {
                StatusTimerNode<TKey> node = new(initialNodeConfig) {
                    Height = StatusNodeHeight,
                    Width = StatusNodeWidth,
                    Origin = new Vector2(StatusNodeWidth / 2, StatusNodeHeight / 2),
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

    private StatusNodeDisplayConfig GetCurrentStatusNodeDisplayConfig() {
        return new StatusNodeDisplayConfig(
            _getShowIcon(),
            _getShowStatusName(),
            _getShowStatusRemaining(),
            _getShowProgress(),
            _getShowStatusRemainingBackground(),
            _getStatusRemainingTextStyle(),
            _getShowActorLetter(),
            _getShowActorName(),
            _getAllowDismissStatus(),
            _getAllowTargetActor(),
            _getAnimationsEnabled(),
            _getIsLocked()
        );
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

    public void UnsubscribeFromNodeActions() {
        if (_onNodeActionTriggered != null) {
            foreach (StatusTimerNode<TKey> node in _allNodes) {
                node.OnStatusNodeActionTriggered -= _onNodeActionTriggered;
            }
        }
    }
}
