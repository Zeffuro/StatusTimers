using KamiToolKit.Classes;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using Newtonsoft.Json;
using StatusTimers.Helpers;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

[JsonObject(MemberSerialization.OptIn)]
public abstract class StatusTimerOverlay<TKey> : SimpleComponentNode {
    private const int PoolSize = 30;
    private const float StatusNodeWidth = 300;
    private const float StatusNodeHeight = 60;

    private readonly List<StatusTimerNode<TKey>> _allNodes = new();
    private readonly NodeKind _nodeKind;

    private readonly IStatusSource<TKey> _source;

    protected readonly Dictionary<TKey, StatusTimerNode<TKey>> Active = new();
    protected readonly Stack<StatusTimerNode<TKey>> Pool = new();

    private NineGridNode _backgroundNode;
    private List<VerticalListNode<StatusTimerNode<TKey>>> _columns = new();

    private TextNode _headerNode;

    private bool _isSetupCompleted;

    private NodeBase _rootContainer;
    private List<HorizontalListNode<StatusTimerNode<TKey>>> _rows = new();

    protected StatusTimerOverlay(IStatusSource<TKey> source, NodeKind nodeKind) {
        _source = source;
        _nodeKind = nodeKind;

        LoadConfig();
    }

    [JsonProperty]
    public bool AllowDismissStatus {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool AllowTargetActor {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool FillRowsFirst {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = true;

    [JsonProperty]
    public GrowDirection GrowDirection {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = GrowDirection.DownRight;

    [JsonProperty]
    public bool IsLocked {
        get;
        set {
            field = value;
            ToggleDrag(field);
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public int ItemsPerLine {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = 8;

    [JsonProperty]
    public bool ShowActorLetter {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowActorName {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowIcon {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowPermaIcons {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusName {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemaining {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemainingBackground {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowProgress {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    public int StatusPadding {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = 4;

    public string Title { get; set; }

    public Vector2 CalculatedOverlaySize { get; private set; }


    public void Setup() {
        if (_isSetupCompleted) {
            return;
        }

        _isSetupCompleted = true;
        OnAttach();

        if (!IsLocked) {
            ToggleDrag(IsLocked);
        }

        OnClickDragComplete = SaveConfig; // Assuming SimpleComponentNode provides this delegate (from previous version)
    }

    protected void OnAttach() {
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

        Services.NativeController.AttachNode(_backgroundNode, this);

        BuildContainers();
    }

    public void OnUpdate() {
        IReadOnlyList<StatusInfo> current = _source.Fetch();

        List<StatusInfo> sortedStatuses = current
            .OrderByDescending(s => s.IsPermanent)
            .ThenByDescending(s => s.RemainingSeconds)
            .ToList();

        int i = 0;
        Dictionary<TKey, StatusTimerNode<TKey>> newActive = new();

        for (; i < sortedStatuses.Count && i < _allNodes.Count; i++) {
            StatusInfo status = sortedStatuses[i];
            StatusTimerNode<TKey> node = _allNodes[i];

            newActive[_source.KeyOf(status)] = node;

            node.StatusInfo = status;
            node.Kind = _nodeKind;
            node.IsVisible = true;
        }

        for (; i < _allNodes.Count; i++) {
            _allNodes[i].IsVisible = false;
        }

        foreach (StatusTimerNode<TKey> node in _allNodes) {
            if (!node.IsVisible) {
            }
        }

        Active.Clear();
        foreach (KeyValuePair<TKey, StatusTimerNode<TKey>> kv in newActive) {
            Active[kv.Key] = kv.Value;
        }

        RecalculateLayout();
    }

    private void BuildContainers()
    {
        CalculatedOverlaySize = CalculateOverlaySize();
        int outerCount = (int)Math.Ceiling(PoolSize / (double)ItemsPerLine);

        if (FillRowsFirst) {
            _rows.Clear();
            SetupContainers(
                () => new VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemVerticalSpacing = StatusPadding
                },
                outer => Services.NativeController.AttachNode(outer, this),
                () => {
                    float innerWidth = (Math.Min(ItemsPerLine, PoolSize) * StatusNodeWidth) +
                                       ((Math.Min(ItemsPerLine, PoolSize) - 1) * StatusPadding);
                    // Inner list (row) height is just the node height
                    float innerHeight = StatusNodeHeight;

                    var list = new HorizontalListNode<StatusTimerNode<TKey>> {
                        Width = innerWidth,
                        Height = innerHeight,
                        IsVisible = true,
                        ItemHorizontalSpacing = StatusPadding
                    };
                    _rows.Add(list);
                    return list;
                },
                (outer, inner) => outer.Add(inner),
                inner => { },
                (inner, node) => inner.Add(node),
                outerCount,
                ItemsPerLine
            );
        } else {
            _columns.Clear();
            SetupContainers(
                () => new HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemHorizontalSpacing = StatusPadding
                },
                outer => Services.NativeController.AttachNode(outer, this),
                () => {
                    float innerWidth = StatusNodeWidth;
                    // Calculate inner list (column) height
                    float innerHeight = (Math.Min(ItemsPerLine, PoolSize) * StatusNodeHeight) +
                                        ((Math.Min(ItemsPerLine, PoolSize) - 1) * StatusPadding);
                    var list = new VerticalListNode<StatusTimerNode<TKey>> {
                        Height = innerWidth,
                        Width = innerHeight,
                        IsVisible = true,
                        ItemVerticalSpacing = StatusPadding,
                        Alignment = VerticalListAnchor.Top
                    };
                    _columns.Add(list);
                    return list;
                },
                (outer, inner) => outer.Add(inner),
                inner => { },
                (inner, node) => inner.Add(node),
                outerCount,
                ItemsPerLine
            );
        }
        Size = CalculatedOverlaySize;

        RecalculateLayout();
    }

    private Vector2 CalculateOverlaySize() {
        float totalWidth;
        float totalHeight;

        int actualItemsPerLine = Math.Min(ItemsPerLine, PoolSize); // Don't exceed PoolSize for items in one line/column

        if (FillRowsFirst) {
            // Rows First: Horizontal lists inside a Vertical list
            // Calculate width of one row
            float singleRowWidth = (actualItemsPerLine * StatusNodeWidth) +
                                   ((actualItemsPerLine - 1) * StatusPadding);

            // Calculate number of rows needed for all 30 items
            int numRows = (int)Math.Ceiling(PoolSize / (double)actualItemsPerLine);

            // Calculate height of all rows
            float allRowsHeight = (numRows * StatusNodeHeight) +
                                  ((numRows - 1) * StatusPadding);

            totalWidth = singleRowWidth;
            totalHeight = allRowsHeight;
        } else {
            // Columns First: Vertical lists inside a Horizontal list
            // Calculate height of one column
            float singleColumnHeight = (actualItemsPerLine * StatusNodeHeight) +
                                       ((actualItemsPerLine - 1) * StatusPadding);

            // Calculate number of columns needed for all 30 items
            int numColumns = (int)Math.Ceiling(PoolSize / (double)actualItemsPerLine);

            // Calculate width of all columns
            float allColumnsWidth = (numColumns * StatusNodeWidth) +
                                    ((numColumns - 1) * StatusPadding);

            totalWidth = allColumnsWidth;
            totalHeight = singleColumnHeight;
        }

        // Add some extra padding for the header/frame if you have one or for general visual spacing
        // This assumes your _backgroundNode's padding (TopOffset, BottomOffset, etc.) is handled by KamiToolKit
        // within the NineGridNode, but you might want some external frame padding too.
        // For simplicity, let's assume the calculated totalWidth/Height is what the root container should be.
        // The background node will adjust to the Size property of this SimpleComponentNode.

        return new Vector2(Math.Max(0, totalWidth), Math.Max(0, totalHeight)); // Ensure non-negative size
    }

    private void RebuildContainers(Action onCompleteCallback = null) {
        if (_rootContainer == null)
        {
            BuildContainers();
            onCompleteCallback?.Invoke();
            return;
        }
        var oldPosition = Position;

        Services.NativeController.DetachNode(_rootContainer, () => {
            foreach (var row in _rows)
            {
                Services.NativeController.DetachNode(row);
            }
            _rows.Clear();

            foreach (var col in _columns)
            {
                Services.NativeController.DetachNode(col);
            }
            _columns.Clear();

            BuildContainers();
            RecalculateLayout();

            Services.Logger.Info($"Position before change, after build/recalculate {Position}, Position stored: {oldPosition}");
            Position = oldPosition;
            Services.Logger.Info($"Position set {Position}");

            onCompleteCallback?.Invoke();
        });
    }

    private void RecalculateLayout()
    {
        bool up   = GrowDirection == GrowDirection.UpLeft  || GrowDirection == GrowDirection.UpRight;
        bool left = GrowDirection == GrowDirection.UpLeft  || GrowDirection == GrowDirection.DownLeft;

        SetVerticalAlignment(up ? VerticalListAnchor.Bottom : VerticalListAnchor.Top);
        SetHorizontalAlignment(left ? HorizontalListAnchor.Right : HorizontalListAnchor.Left);
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
        _allNodes.Clear();

        TOuter outer = createOuter();

        for (int i = 0, nodeIndex = 0; i < outerCount && nodeIndex < PoolSize; i++) {
            TInner inner = createInner();
            configureInner(inner);
            addInnerToOuter(outer, inner);

            for (int j = 0; j < itemsPerInner && nodeIndex < PoolSize; j++, nodeIndex++) {
                StatusTimerNode<TKey> node = new() {
                    Height = StatusNodeHeight,
                    Width = StatusNodeWidth,
                    IsVisible = false,
                    Parent = this
                };
                addNodeToInner(inner, node);
                _allNodes.Add(node);
            }
        }

        // Attach only the outermost container once
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

    private void ToggleDrag(bool isLocked) {
        if (!isLocked) {
            EnableClickDrag(true);
            if (_backgroundNode != null) {
                _backgroundNode.IsVisible = true;
            }
        }
        else {
            DisableClickDrag(true);
            if (_backgroundNode != null) {
                _backgroundNode.IsVisible = false;
            }
        }
    }

    public void LoadConfig() {
        string configPath = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Load(configPath);
        Services.Logger.Info($"Loaded overlay '{_nodeKind.ToString()}' from {configPath}");
    }

    public void SaveConfig() {
        string configPath = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Save(configPath);
        Services.Logger.Verbose($"Saved overlay '{_nodeKind.ToString()}' to {configPath}");
    }
}

public enum GrowDirection {
    DownRight,
    DownLeft,
    UpRight,
    UpLeft
}
