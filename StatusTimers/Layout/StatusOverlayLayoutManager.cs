using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Logic;
using StatusTimers.Models;
using StatusTimers.Nodes.LayoutNodes;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Layout;

public class StatusOverlayLayoutManager<TKey>(
    StatusTimerOverlay<TKey> ownerOverlay,
    Func<StatusTimerOverlayConfig> getOverlayConfig)
    : IDisposable
    where TKey : notnull {

    private NineGridNode? _backgroundNode;

    private StatusTimerNode<TKey>.StatusNodeActionHandler? _onNodeActionTriggered;
    private LayoutListNode? _rootContainer;

    public Vector2 CalculatedOverlaySize { get; private set; }

    public LayoutListNode? RootContainer => _rootContainer;
    public NineGridNode? BackgroundNode => _backgroundNode;

    public void SetNodeActionHandler(StatusTimerNode<TKey>.StatusNodeActionHandler handler) {
        _onNodeActionTriggered = handler;
    }

    public void InitializeLayout() {
        var config = getOverlayConfig();

        CalculatedOverlaySize = OverlayLayoutHelper.CalculateOverlaySize(config);

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

        GlobalServices.NativeController.AttachNode(_backgroundNode, ownerOverlay);

        BuildContainers();
    }

    private void BuildContainers()
    {
        var config = getOverlayConfig();

        CalculatedOverlaySize = OverlayLayoutHelper.CalculateOverlaySize(config);

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

        _rootContainer.RecalculateLayout();

        GlobalServices.NativeController.AttachNode(_rootContainer, ownerOverlay);
        ownerOverlay.Size = CalculatedOverlaySize;
        ToggleBackground(ownerOverlay.IsLocked);
    }

    public void RecalculateLayout()
    {
        var config = getOverlayConfig();
        if (_rootContainer == null || _backgroundNode == null) {
            return;
        }

        if (_rootContainer is HybridDirectionalFlexNode flexNode)
        {
            flexNode.GrowDirection = (FlexGrowDirection)config.GrowDirection;
            flexNode.ItemsPerLine = config.ItemsPerLine;
            flexNode.FillRowsFirst = config.FillRowsFirst;
            flexNode.HorizontalPadding = config.StatusHorizontalPadding;
            flexNode.VerticalPadding = config.StatusVerticalPadding;
        }

        CalculatedOverlaySize = OverlayLayoutHelper.CalculateOverlaySize(config);
        _rootContainer.Width = CalculatedOverlaySize.X;
        _rootContainer.Height = CalculatedOverlaySize.Y;
        _backgroundNode.Size = CalculatedOverlaySize;

        foreach (var node in _rootContainer.GetNodes<StatusTimerNode<TKey>>())
        {
            node.Width = config.RowWidth;
            node.Height = config.RowHeight;
        }

        _rootContainer.RecalculateLayout();
    }

    public void ToggleBackground(bool isLocked) {
        if (_backgroundNode != null) {
            _backgroundNode.IsVisible = !isLocked;
        }
    }

    public void UpdateAllNodesDisplay(string property) {
        if (_rootContainer == null) {
            return;
        }
        foreach (StatusTimerNode<TKey> node in _rootContainer.GetNodes<StatusTimerNode<TKey>>()) {
            node.ApplyOverlayConfig(property);
        }
    }

    public void UpdateNodeContent(List<StatusInfo> filteredList, NodeKind nodeKind) {
        _rootContainer?.SyncWithListData(
            filteredList,
            node => node.StatusInfo,
            data => {
                var node = new StatusTimerNode<TKey>(getOverlayConfig)
                {
                    Height = getOverlayConfig().RowHeight,
                    Width = getOverlayConfig().RowWidth,
                    IsVisible = true,
                    StatusInfo = data,
                    Kind = nodeKind
                };
                if (_onNodeActionTriggered != null) {
                    node.OnStatusNodeActionTriggered += _onNodeActionTriggered;
                }
                return node;
            });

        foreach (var node in _rootContainer?.GetNodes<StatusTimerNode<TKey>>() ?? [])
        {
            var latestInfo = filteredList.FirstOrDefault(info => info.Id == node.StatusInfo.Id && info.GameObjectId == node.StatusInfo.GameObjectId);
            if (latestInfo != null)
            {
                node.StatusInfo = latestInfo;
            }
        }

        var comparison = StatusSorter.GetNodeComparison<TKey>(
            getOverlayConfig().PrimarySort,
            getOverlayConfig().PrimarySortOrder,
            getOverlayConfig().SecondarySort,
            getOverlayConfig().SecondarySortOrder,
            getOverlayConfig().TertiarySort,
            getOverlayConfig().TertiarySortOrder
        );

        _rootContainer?.ReorderNodes((a, b) =>
            comparison(
                (StatusTimerNode<TKey>)a,
                (StatusTimerNode<TKey>)b
            )
        );
    }

    public void UnsubscribeFromNodeActions() {
        if (_rootContainer == null) {
            return;
        }
        foreach (StatusTimerNode<TKey> node in _rootContainer.GetNodes<StatusTimerNode<TKey>>()) {
            node.OnStatusNodeActionTriggered -= _onNodeActionTriggered;
        }
    }
    public void Dispose() {
    }
}
