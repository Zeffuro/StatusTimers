using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Nodes;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Layout;

public class StatusOverlayLayoutManager<TKey> : IDisposable {
    private bool isDisposed = false;

    private readonly List<StatusTimerNode<TKey>> _allNodes = new();
    private readonly List<VerticalListNode> _columns = new();
    private readonly Func<StatusTimerOverlayConfig?> _getOverlayConfig;

    private readonly StatusTimerOverlay<TKey> _ownerOverlay;
    private readonly List<HorizontalListNode> _rows = new();

    private NineGridNode _backgroundNode;

    private StatusTimerNode<TKey>.StatusNodeActionHandler _onNodeActionTriggered;
    private HybridDirectionalFlexNode _rootContainer;

    public StatusOverlayLayoutManager(
        StatusTimerOverlay<TKey> ownerOverlay,
        Func<StatusTimerOverlayConfig?> getOverlayConfig) {
        _ownerOverlay = ownerOverlay;
        _getOverlayConfig = getOverlayConfig;
    }

    public Vector2 CalculatedOverlaySize { get; private set; }

    public HybridDirectionalFlexNode RootContainer => _rootContainer;
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

    public void BuildContainers()
    {
        _allNodes.Clear();

        var config = _getOverlayConfig();
        CalculatedOverlaySize = CalculateOverlaySize();

        _rootContainer = new HybridDirectionalFlexNode
        {
            X = 0,
            Y = 0,
            NodeId = 3,
            Width = CalculatedOverlaySize.X,
            Height = CalculatedOverlaySize.Y,
            IsVisible = true,
            HorizontalPadding = config.StatusHorizontalPadding,
            VerticalPadding = config.StatusVerticalPadding,
            GrowDirection = (FlexGrowDirection)config.GrowDirection,
            ItemsPerLine = config.ItemsPerLine,
            FillRowsFirst = config.FillRowsFirst
        };

        for (int i = 0; i < config.MaxStatuses; i++)
        {
            var node = new StatusTimerNode<TKey>(_getOverlayConfig)
            {
                Height = config.RowHeight,
                Width = config.RowWidth,
                IsVisible = false
            };
            _rootContainer.AddNode(node);
            _allNodes.Add(node);

            if (_onNodeActionTriggered != null) {
                node.OnStatusNodeActionTriggered += _onNodeActionTriggered;
            }
        }

        _rootContainer.RecalculateLayout();

        GlobalServices.NativeController.AttachNode(_rootContainer, _ownerOverlay);
        _ownerOverlay.Size = CalculatedOverlaySize;
        ToggleBackground(_ownerOverlay.IsLocked);
    }

    public void RecalculateLayout()
    {
        var config = _getOverlayConfig();
        if (_rootContainer != null)
        {
            _rootContainer.GrowDirection = (FlexGrowDirection)config.GrowDirection;
            _rootContainer.ItemsPerLine = config.ItemsPerLine;
            _rootContainer.FillRowsFirst = config.FillRowsFirst;
            _rootContainer.HorizontalPadding = config.StatusHorizontalPadding;
            _rootContainer.VerticalPadding = config.StatusVerticalPadding;

            CalculatedOverlaySize = CalculateOverlaySize();
            _rootContainer.Width = CalculatedOverlaySize.X;
            _rootContainer.Height = CalculatedOverlaySize.Y;
            _backgroundNode.Size = CalculatedOverlaySize;

            foreach (var node in _allNodes)
            {
                node.Width = config.RowWidth;
                node.Height = config.RowHeight;
            }

            _rootContainer.RecalculateLayout();
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
