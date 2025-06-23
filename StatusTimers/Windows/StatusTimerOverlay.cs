using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
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
public abstract class StatusTimerOverlay<TKey> : SimpleComponentNode, IOverlayConfiguration {
    private const float StatusNodeWidth = 300;
    private const float StatusNodeHeight = 60;

    private readonly List<StatusTimerNode<TKey>> _allNodes = new();
    private readonly NodeKind _nodeKind;

    protected readonly Dictionary<TKey, StatusTimerNode<TKey>> Active = new();
    protected readonly Stack<StatusTimerNode<TKey>> Pool = new();

    private List<StatusInfo> _dummyActiveStatuses = new();
    private DateTime _lastDummyUpdateTime;
    private static readonly Random _rand = new();

    private static readonly List<DummyStatusTemplate> _combinedDummyTemplates = new() {
        new DummyStatusTemplate(1239, 213409, "Embolden", 20f, StatusCategory.Buff),
        new DummyStatusTemplate(786, 212578, "Battle Litany", 15f, StatusCategory.Buff),
        new DummyStatusTemplate(1822, 213709, "Technical Finish", 20f, StatusCategory.Buff),
        new DummyStatusTemplate(2174, 212532, "Brotherhood", 15f, StatusCategory.Buff),
        new DummyStatusTemplate(2912, 217101, "Vulnerability Up", 30f, StatusCategory.Buff)
    };

    private static readonly List<DummyStatusTemplate> _multiDotDummyTemplates = new() {
        new DummyStatusTemplate(1205, 212616, "Caustic Bite", 45f, StatusCategory.Debuff),
        new DummyStatusTemplate(1206, 212617, "Stormbite", 45f, StatusCategory.Debuff),
        new DummyStatusTemplate(1228, 213304, "Higanbana", 60, StatusCategory.Debuff),
        new DummyStatusTemplate(3871, 212661, "High Thunder", 30, StatusCategory.Debuff)
    };

    private NineGridNode _backgroundNode;
    private List<VerticalListNode<StatusTimerNode<TKey>>> _columns = new();

    private TextNode _headerNode;

    private bool _isSetupCompleted;

    private NodeBase _rootContainer;
    private List<HorizontalListNode<StatusTimerNode<TKey>>> _rows = new();

    protected IStatusSource<TKey> Source;

    protected StatusTimerOverlay(NodeKind nodeKind) {
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
    public bool AnimationsEnabled {
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
    } = false;

    [JsonProperty]
    public bool FilterStatuses {
        get;
        set {
            field = value;
            SaveConfig();
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

    public bool IsPreviewEnabled {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = false;

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
    } = 16;

    [JsonProperty]
    public int MaxStatuses {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = 30;

    [JsonProperty]
    public int ScaleInt {
        get;
        set {
            field = value;
            Scale = new Vector2(ScaleInt * 0.01f);
            RebuildContainers(SaveConfig);
        }
    } = 100;

    [JsonProperty]
    public bool ShowIcon {
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

    public int StatusHorizontalPadding {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = 4;

    public int StatusVerticalPadding {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = 4;

    [JsonProperty]
    public SortCriterion PrimarySort {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortCriterion.StatusType;

    [JsonProperty]
    public SortCriterion SecondarySort {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortCriterion.OwnStatusFirst;

    [JsonProperty]
    public SortCriterion TertiarySort {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortCriterion.PartyPriority;

    [JsonProperty]
    public SortOrder PrimarySortOrder {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder SecondarySortOrder {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder TertiarySortOrder {
        get;
        set {
            field = value;
            RebuildContainers(SaveConfig);
        }
    } = SortOrder.Ascending;

    public string Title { get; set; }

    public Vector2 CalculatedOverlaySize { get; private set; }

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
    public bool ShowPermaIcons {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool StatusAsItemName {
        get;
        set {
            field = value;
            SaveConfig();
        }
    } = true;

    public override Vector4 Color { get; set; }
    public override Vector3 AddColor { get; set; }
    public override Vector3 MultiplyColor { get; set; }

    public void Setup() {
        if (_isSetupCompleted) {
            return;
        }
        OnAttach();

        if (!IsLocked) {
            ToggleDrag(IsLocked);
        }

        Scale = new Vector2(ScaleInt * 0.01f);

        OnClickDragComplete = SaveConfig;
        _isSetupCompleted = true;
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
        IReadOnlyList<StatusInfo> current;

        if (IsPreviewEnabled) {
            if (_dummyActiveStatuses.Count == 0 && _allNodes.Count > 0) {
                InitializeDummyStatuses();
                _lastDummyUpdateTime = DateTime.Now;
            } else if (_dummyActiveStatuses.Count > 0) {
                UpdateDummyStatusTimers();
            }
            current = _dummyActiveStatuses;
        } else {
            current = Source.Fetch(this);
            if (_dummyActiveStatuses.Count > 0) {
                _dummyActiveStatuses.Clear();
            }
        }

        IEnumerable<StatusInfo> filteredStatuses = current;
        if (_nodeKind == NodeKind.Combined && !ShowPermaIcons)
        {
            filteredStatuses = filteredStatuses.Where(s => !s.IsPermanent);
        }

        IOrderedEnumerable<StatusInfo>? sortedStatuses = null;
        IEnumerable<StatusInfo> initialList = filteredStatuses;

        if (PrimarySort != SortCriterion.None)
        {
            sortedStatuses = ApplySingleSort(initialList, PrimarySort, PrimarySortOrder);
        }
        else
        {
            sortedStatuses = initialList.OrderBy(s => 0);
        }

        if (SecondarySort != SortCriterion.None)
        {
            sortedStatuses = ApplyThenBySort(sortedStatuses, SecondarySort, SecondarySortOrder);
        }

        if (TertiarySort != SortCriterion.None)
        {
            sortedStatuses = ApplyThenBySort(sortedStatuses, TertiarySort, TertiarySortOrder);
        }

        List<StatusInfo> finalSortedList = sortedStatuses
            .Take(MaxStatuses)
            .ToList();

        int i = 0;
        Dictionary<TKey, StatusTimerNode<TKey>> newActive = new();

        for (; i < finalSortedList.Count && i < _allNodes.Count; i++) {
            StatusInfo status = finalSortedList[i];
            StatusTimerNode<TKey> node = _allNodes[i];

            newActive[Source.KeyOf(status)] = node;

            node.StatusInfo = status;
            node.Kind = _nodeKind;
            if (!node.IsVisible) {
                node.IsVisible = true;
            }
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

    private void BuildContainers() {
        CalculatedOverlaySize = CalculateOverlaySize();
        int outerCount = (int)Math.Ceiling(MaxStatuses / (double)ItemsPerLine);

        if (FillRowsFirst) {
            _rows.Clear();
            SetupContainers(
                () => new VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemVerticalSpacing = StatusVerticalPadding
                },
                outer => Services.NativeController.AttachNode(outer, this),
                () => {
                    float innerWidth = Math.Min(ItemsPerLine, MaxStatuses) * StatusNodeWidth +
                                       (Math.Min(ItemsPerLine, MaxStatuses) - 1) * StatusHorizontalPadding;
                    float innerHeight = StatusNodeHeight;

                    HorizontalListNode<StatusTimerNode<TKey>> list = new() {
                        Width = innerWidth,
                        Height = innerHeight,
                        IsVisible = true,
                        ItemHorizontalSpacing = StatusHorizontalPadding
                    };
                    _rows.Add(list);
                    return list;
                },
                (outer, inner) => outer.AddNode(inner),
                inner => {
                    // If we need to edit the StatusTimerNode
                    //AddLabelTimeLine(inner);
                },
                (inner, node) => {
                    inner.AddNode(node);
                    node.OuterContainer = inner;
                },
                outerCount,
                ItemsPerLine
            );
        }
        else {
            _columns.Clear();
            SetupContainers(
                () => new HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> {
                    Width = CalculatedOverlaySize.X,
                    Height = CalculatedOverlaySize.Y,
                    IsVisible = true,
                    ItemHorizontalSpacing = StatusHorizontalPadding
                },
                outer => Services.NativeController.AttachNode(outer, this),
                () => {
                    float innerWidth = StatusNodeWidth;
                    float innerHeight = Math.Min(ItemsPerLine, MaxStatuses) * StatusNodeHeight +
                                        (Math.Min(ItemsPerLine, MaxStatuses) - 1) * StatusHorizontalPadding;
                    VerticalListNode<StatusTimerNode<TKey>> list = new() {
                        Height = innerHeight,
                        Width = innerWidth,
                        IsVisible = true,
                        ItemVerticalSpacing = StatusVerticalPadding,
                        Alignment = VerticalListAnchor.Top
                    };
                    _columns.Add(list);
                    return list;
                },
                (outer, inner) => outer.AddNode(inner),
                inner => {
                    // If we need to edit the StatusTimerNode
                    //AddLabelTimeLine(inner);
                },
                (inner, node) => {
                    inner.AddNode(node);
                    node.OuterContainer = inner;
                },
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

        int actualItemsPerLine = Math.Min(ItemsPerLine, MaxStatuses);

        if (FillRowsFirst) {
            float singleRowWidth = actualItemsPerLine * StatusNodeWidth +
                                   (actualItemsPerLine - 1) * StatusHorizontalPadding;

            int numRows = (int)Math.Ceiling(MaxStatuses / (double)actualItemsPerLine);

            float allRowsHeight = numRows * StatusNodeHeight +
                                  (numRows - 1) * StatusVerticalPadding;

            totalWidth = singleRowWidth;
            totalHeight = allRowsHeight;
        }
        else {
            float singleColumnHeight = actualItemsPerLine * StatusNodeHeight +
                                       (actualItemsPerLine - 1) * StatusVerticalPadding;

            int numColumns = (int)Math.Ceiling(MaxStatuses / (double)actualItemsPerLine);

            float allColumnsWidth = numColumns * StatusNodeWidth +
                                    (numColumns - 1) * StatusHorizontalPadding;

            totalWidth = allColumnsWidth;
            totalHeight = singleColumnHeight;
        }

        return new Vector2(Math.Max(0, totalWidth), Math.Max(0, totalHeight));
    }

    private void FinalizeOverlayPositionAndSize(Vector2 newPosition, Vector2 newSize) {
        Size = newSize;
        if (_backgroundNode != null) {
            _backgroundNode.Size = newSize;
        }

        // Magic
        MagicCornerYeetFix();
    }

    private void MagicCornerYeetFix() {
        Position += new Vector2(1, 1);
        Position -= new Vector2(1, 1);
    }

    private void RebuildContainers(Action onCompleteCallback = null) {
        if (_rootContainer == null) {
            BuildContainers();
            FinalizeOverlayPositionAndSize(Position, CalculatedOverlaySize);
            onCompleteCallback?.Invoke();
            return;
        }

        Vector2 oldPosition = Position;

        Services.NativeController.DetachNode(_rootContainer, () => {
            foreach (HorizontalListNode<StatusTimerNode<TKey>> row in _rows) {
                Services.NativeController.DetachNode(row);
            }

            _rows.Clear();

            foreach (VerticalListNode<StatusTimerNode<TKey>> col in _columns) {
                Services.NativeController.DetachNode(col);
            }

            _columns.Clear();

            BuildContainers();

            Services.Framework.RunOnTick(() => {
                RecalculateLayout();
                FinalizeOverlayPositionAndSize(oldPosition, CalculatedOverlaySize);
                onCompleteCallback?.Invoke();
            }, delayTicks: 3);
        });
    }

    private void RecalculateLayout() {
        bool up = GrowDirection is GrowDirection.UpLeft or GrowDirection.UpRight;
        bool left = GrowDirection is GrowDirection.UpLeft or GrowDirection.DownLeft;

        SetVerticalAlignment(up ? VerticalListAnchor.Bottom : VerticalListAnchor.Top);
        SetHorizontalAlignment(left ? HorizontalListAnchor.Right : HorizontalListAnchor.Left);

        if (_rootContainer != null) {
            float rootContainerOffsetX = 0;
            float rootContainerOffsetY = 0;

            if (up) {
                rootContainerOffsetY = -StatusNodeHeight;
            }

            if (left) {
                rootContainerOffsetX = -StatusNodeWidth;
            }

            _rootContainer.X = rootContainerOffsetX;
            _rootContainer.Y = rootContainerOffsetY;

            switch (_rootContainer)
            {
                case VerticalListNode<HorizontalListNode<StatusTimerNode<TKey>>> verticalRoot:
                    verticalRoot.RecalculateLayout();
                    break;
                case HorizontalListNode<VerticalListNode<StatusTimerNode<TKey>>> horizontalRoot:
                    horizontalRoot.RecalculateLayout();
                    break;
            }
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
        _allNodes.Clear();

        TOuter outer = createOuter();

        for (int i = 0, nodeIndex = 0; i < outerCount && nodeIndex < MaxStatuses; i++) {
            TInner inner = createInner();
            configureInner(inner);
            addInnerToOuter(outer, inner);

            for (int j = 0; j < itemsPerInner && nodeIndex < MaxStatuses; j++, nodeIndex++) {
                StatusTimerNode<TKey> node = new() {
                    Height = StatusNodeHeight,
                    Width = StatusNodeWidth,
                    Origin = new Vector2(StatusNodeWidth / 2, StatusNodeHeight / 2),
                    IsVisible = false,
                    Parent = this
                };
                addNodeToInner(inner, node);
                _allNodes.Add(node);
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

    private void ToggleDrag(bool isLocked) {
        if (!isLocked) {
            EnableClickDrag(true);
            if (_backgroundNode != null) {
                _backgroundNode.IsVisible = true;
                _backgroundNode.AddFlags(NodeFlags.Focusable, NodeFlags.IsTopNode);
            }
        }
        else {
            DisableClickDrag(false);
            if (_backgroundNode != null) {
                _backgroundNode.IsVisible = false;
                _backgroundNode.RemoveFlags(NodeFlags.Focusable, NodeFlags.IsTopNode);
            }
        }
    }

    private void InitializeDummyStatuses() {
        _dummyActiveStatuses.Clear();

        for (int i = 0; i < MaxStatuses; i++) {
            _dummyActiveStatuses.Add(CreateNewDummyStatus(i));
        }
    }

    private void UpdateDummyStatusTimers() {
        TimeSpan elapsed = DateTime.Now - _lastDummyUpdateTime;
        _lastDummyUpdateTime = DateTime.Now;

        float deltaSeconds = (float)elapsed.TotalSeconds;

        List<StatusInfo> updatedList = new();

        foreach (StatusInfo status in _dummyActiveStatuses) {
            if (status.IsPermanent) {
                updatedList.Add(status);
                continue;
            }

            float newRemaining = status.RemainingSeconds - deltaSeconds;
            if (newRemaining <= 0) {
                updatedList.Add(CreateNewDummyStatus());
            } else {
                updatedList.Add(new StatusInfo(
                    id: status.Id,
                    iconId: status.IconId,
                    name: status.Name,
                    remainingSeconds: newRemaining,
                    maxSeconds: status.MaxSeconds,
                    gameObjectId: status.GameObjectId,
                    selfInflicted: status.SelfInflicted,
                    stacks: status.Stacks,
                    partyPriority: status.PartyPriority,
                    isPermanent: status.IsPermanent,
                    actorName: status.ActorName,
                    enemyLetter: status.EnemyLetter
                ));
            }
        }
        _dummyActiveStatuses = updatedList;
    }

    private StatusInfo CreateNewDummyStatus(int? initialIndex = null) {
        uint dummyId;
        uint dummyIconId;
        string dummyName;
        float maxSeconds;
        bool isPermanent;
        bool selfInflicted;
        StatusCategory statusCategory;

        uint dummyStacks = (uint)_rand.Next(1, 4);

        ulong gameObjectIdToUse = 0UL;
        string? actorName = null;
        char? enemyLetter = null;

        DummyStatusTemplate selectedTemplate;

        if (_nodeKind == NodeKind.MultiDoT) {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = _multiDotDummyTemplates[index % _multiDotDummyTemplates.Count];

                ulong baseActorId = 1000UL;
                gameObjectIdToUse = baseActorId + (ulong)(index / ItemsPerLine);
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            } else {
                selectedTemplate = _multiDotDummyTemplates[_rand.Next(0, _multiDotDummyTemplates.Count)];

                ulong baseActorId = 1000UL;
                int numDummyActors = MaxStatuses / ItemsPerLine;
                gameObjectIdToUse = baseActorId + (ulong)_rand.Next(0, Math.Max(1, numDummyActors));
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        } else {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = _combinedDummyTemplates[index % _combinedDummyTemplates.Count];

                gameObjectIdToUse = 0UL;
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent && index % 5 == 0;
                statusCategory = selectedTemplate.StatusType;
            } else {
                selectedTemplate = _combinedDummyTemplates[_rand.Next(0, _combinedDummyTemplates.Count)];

                gameObjectIdToUse = 0UL;
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        }

        dummyId = selectedTemplate.Id;
        dummyIconId = selectedTemplate.IconId;
        dummyName = selectedTemplate.Name;
        maxSeconds = selectedTemplate.MaxSeconds;
        selfInflicted = _rand.Next(100) < 50;

        float remainingSeconds = maxSeconds * (float)(_rand.NextDouble() * 0.8 + 0.1);

        return new StatusInfo(
            id: dummyId,
            iconId: dummyIconId,
            name: dummyName,
            remainingSeconds: remainingSeconds,
            maxSeconds: maxSeconds,
            gameObjectId: gameObjectIdToUse,
            selfInflicted: selfInflicted,
            stacks: dummyStacks,
            partyPriority: 0,
            isPermanent: isPermanent,
            actorName: actorName,
            enemyLetter: enemyLetter,
            category: statusCategory
        );
    }

    private IOrderedEnumerable<StatusInfo> ApplySingleSort(IEnumerable<StatusInfo> list, SortCriterion criterion, SortOrder order) {
        return criterion switch {
            SortCriterion.StatusType => order == SortOrder.Ascending
                ? list.OrderBy(status => status.StatusType)
                : list.OrderByDescending(status => status.StatusType),
            SortCriterion.TimeRemaining => order == SortOrder.Ascending
                ? list.OrderBy(status => status.RemainingSeconds)
                : list.OrderByDescending(status => status.RemainingSeconds),
            SortCriterion.OwnStatusFirst => order == SortOrder.Ascending
                ? list.OrderByDescending(status => status.SelfInflicted)
                : list.OrderBy(status => status.SelfInflicted),
            SortCriterion.PartyPriority => order == SortOrder.Ascending
                ? list.OrderByDescending(status => status.SelfInflicted)
                : list.OrderBy(status => status.SelfInflicted),
            _ => list.OrderBy(status => 0)
        };
    }

    private IOrderedEnumerable<StatusInfo> ApplyThenBySort(IOrderedEnumerable<StatusInfo> orderedList, SortCriterion criterion, SortOrder order) {
        if (orderedList == null) {
            return null;
        }

        return criterion switch {
            SortCriterion.StatusType => order == SortOrder.Ascending
                ? orderedList.ThenBy(status => status.StatusType)
                : orderedList.ThenByDescending(status => status.StatusType),
            SortCriterion.TimeRemaining => order == SortOrder.Ascending
                ? orderedList.ThenBy(status => status.RemainingSeconds)
                : orderedList.ThenByDescending(status => status.RemainingSeconds),
            SortCriterion.OwnStatusFirst => order == SortOrder.Ascending
                ? orderedList.ThenByDescending(status => status.SelfInflicted)
                : orderedList.ThenBy(status => status.SelfInflicted),
            SortCriterion.PartyPriority => order == SortOrder.Ascending
                ? orderedList.ThenByDescending(status => status.SelfInflicted)
                : orderedList.ThenBy(status => status.SelfInflicted),
            _ => orderedList
        };
    }

    public void LoadConfig() {
        string configPath = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Load(configPath);
        Services.Logger.Info($"Loaded overlay '{_nodeKind.ToString()}' from {configPath}");
    }

    public void SaveConfig() {
        if (!_isSetupCompleted) {
            return;
        }
        string configPath = Path.Combine(Services.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Save(configPath);
        Services.Logger.Verbose($"Saved overlay '{_nodeKind.ToString()}' to {configPath}");
    }

    public void OnDispose() {
        SaveConfig();
        _isSetupCompleted = false;
    }

    private void AddLabelTimeLine(NodeBase node) {
        // Future Zeff, this always goes on a parent
        var labels = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddLabel(1, 10, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(20, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build();

        node.AddTimeline(labels);
    }


}


public record DummyStatusTemplate(
    uint Id,
    uint IconId,
    string Name,
    float MaxSeconds,
    StatusCategory StatusType,
    bool IsPermanent = false
);

public enum GrowDirection {
    DownRight = 0,
    DownLeft = 1,
    UpRight = 2,
    UpLeft = 3
}

public enum SortCriterion
{
    None = 0,
    StatusType = 1,
    TimeRemaining = 2,
    OwnStatusFirst = 3,
    PartyPriority = 4
}

public enum SortOrder
{
    Ascending = 0,
    Descending = 1
}
