using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using Newtonsoft.Json;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Models;
using StatusTimers.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using GameStatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

[JsonObject(MemberSerialization.OptIn)]
public abstract class StatusTimerOverlay<TKey> : SimpleComponentNode, IOverlayConfiguration {
    private const float StatusNodeWidth = 300;
    private const float StatusNodeHeight = 60;

    private readonly NodeKind _nodeKind;

    private readonly IStatusSource<TKey> _source;

    protected readonly Dictionary<TKey, StatusTimerNode<TKey>> Active = new();
    protected readonly Stack<StatusTimerNode<TKey>> Pool = new();
    private StatusDataSourceManager<TKey> _dataSourceManager;

    private bool _isConfigLoading;
    private bool _isSetupCompleted;

    private StatusOverlayLayoutManager<TKey> _layoutManager;

    protected StatusTimerOverlay(NodeKind nodeKind, IStatusSource<TKey> source) {
        _nodeKind = nodeKind;
        _source = source;

        _layoutManager = new StatusOverlayLayoutManager<TKey>(
            this,
            _nodeKind,
            () => MaxStatuses,
            () => ItemsPerLine,
            () => StatusHorizontalPadding,
            () => StatusVerticalPadding,
            () => GrowDirection,
            () => IsLocked,
            () => StatusRemainingTextStyle,
            () => ShowIcon,
            () => ShowStatusName,
            () => ShowStatusRemaining,
            () => ShowProgress,
            () => ShowStatusRemainingBackground,
            () => ShowActorLetter,
            () => ShowActorName,
            () => AllowDismissStatus,
            () => AllowTargetActor,
            () => AnimationsEnabled,
            () => FillRowsFirst
        );

        _layoutManager.SetNodeActionHandler(HandleStatusNodeAction);

        _dataSourceManager = new StatusDataSourceManager<TKey>(
            _source,
            _nodeKind,
            () => IsPreviewEnabled,
            () => ShowPermaIcons,
            () => MaxStatuses,
            () => ItemsPerLine,
            () => PrimarySort,
            () => PrimarySortOrder,
            () => SecondarySort,
            () => SecondarySortOrder,
            () => TertiarySort,
            () => TertiarySortOrder
        );
    }

    [JsonProperty]
    public bool FilterStatuses {
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
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    public Vector2 CalculatedOverlaySize => _layoutManager.CalculatedOverlaySize;

    public override Vector4 Color { get; set; }
    public override Vector3 AddColor { get; set; }
    public override Vector3 MultiplyColor { get; set; }

    [JsonProperty]
    public bool AllowDismissStatus {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool AllowTargetActor {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool AnimationsEnabled {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool FillRowsFirst {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = false;

    [JsonProperty]
    public GrowDirection GrowDirection {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
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
            _layoutManager.ToggleDrag(field);
            if (!field) {
                EnableClickDrag(true);
            }
            else {
                DisableClickDrag();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public int ItemsPerLine {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = 16;

    [JsonProperty]
    public int MaxStatuses {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = 30;

    [JsonProperty]
    public int ScaleInt {
        get;
        set {
            field = value;
            Scale = new Vector2(ScaleInt * 0.01f);
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = 100;

    [JsonProperty]
    public bool ShowStatusName {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemaining {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemainingBackground {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowProgress {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public int StatusHorizontalPadding {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = 4;

    [JsonProperty]
    public int StatusVerticalPadding {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = 4;

    [JsonProperty]
    public TextStyle StatusRemainingTextStyle {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }
        }
    } = new() {
        Width = 120,
        Height = 22,
        FontSize = 20,
        FontType = FontType.Axis,
        TextColor = ColorHelper.GetColor(50),
        TextOutlineColor = ColorHelper.GetColor(53),
        TextFlags = TextFlags.Edge
    };

    [JsonProperty]
    public SortCriterion PrimarySort {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortCriterion.StatusType;

    [JsonProperty]
    public SortCriterion SecondarySort {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortCriterion.OwnStatusFirst;

    [JsonProperty]
    public SortCriterion TertiarySort {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortCriterion.PartyPriority;

    [JsonProperty]
    public SortOrder PrimarySortOrder {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder SecondarySortOrder {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder TertiarySortOrder {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                RebuildContainers(SaveConfig);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public bool ShowActorLetter {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

            SaveConfig();
        }
    } = true;

    [JsonProperty]
    public bool ShowActorName {
        get;
        set {
            field = value;
            if (!_isConfigLoading) {
                _layoutManager.UpdateAllNodesDisplay();
            }

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

    public void Setup() {
        if (_isSetupCompleted) {
            return;
        }

        _isConfigLoading = true;
        LoadConfig();
        _isConfigLoading = false;

        _layoutManager.InitializeLayout();
        Size = _layoutManager.CalculatedOverlaySize;

        if (!IsLocked) {
            ToggleDrag(IsLocked);
        }

        Scale = new Vector2(ScaleInt * 0.01f);

        OnClickDragComplete = SaveConfig;
        _isSetupCompleted = true;
    }

    public void OnUpdate() {
        List<StatusInfo> finalSortedList = _dataSourceManager.FetchAndProcessStatuses(this);

        _layoutManager.UpdateNodeContent(finalSortedList, _nodeKind);

        Active.Clear();
        foreach (StatusTimerNode<TKey> node in _layoutManager.AllNodes) {
            if (node.IsVisible && node.StatusInfo.Id != 0) {
                Active[_dataSourceManager.KeyOf(node.StatusInfo)] = node;
            }
        }

        _layoutManager.RecalculateLayout();
    }

    private void FinalizeOverlayPositionAndSize(Vector2 newPosition, Vector2 newSize) {
        Size = newSize;

        // Magic
        MagicCornerYeetFix();
    }

    private void MagicCornerYeetFix() {
        Position += new Vector2(1, 1);
        Position -= new Vector2(1, 1);
    }

    private void RebuildContainers(Action onCompleteCallback = null) {
        _layoutManager.RebuildContainers(() => {
            Size = _layoutManager.CalculatedOverlaySize;
            FinalizeOverlayPositionAndSize(Position, _layoutManager.CalculatedOverlaySize);
            onCompleteCallback?.Invoke();
        });
    }

    private void ToggleDrag(bool isLocked) {
        if (!isLocked) {
            EnableClickDrag(true);
        }
        else {
            DisableClickDrag();
        }

        _layoutManager.ToggleDrag(isLocked);
    }

    private void HandleStatusNodeAction(uint statusId, ulong? gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget) {
        if (nodeKind == NodeKind.Combined && allowDismiss) {
            GameStatusManager.ExecuteStatusOff(statusId);
        }

        if (nodeKind == NodeKind.MultiDoT && gameObjectToTargetId.HasValue && allowTarget) {
            GlobalServices.TargetManager.Target =
                GlobalServices.ObjectTable.FirstOrDefault(gameObject =>
                    gameObject is not null && gameObject.GameObjectId == gameObjectToTargetId.Value);
        }
    }

    public void LoadConfig() {
        string configPath = Path.Combine(GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Load(configPath);
        GlobalServices.Logger.Info($"Loaded overlay '{_nodeKind.ToString()}' from {configPath}");
    }

    public void SaveConfig() {
        if (!_isSetupCompleted) {
            return;
        }

        string configPath = Path.Combine(GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Save(configPath);
        GlobalServices.Logger.Verbose($"Saved overlay '{_nodeKind.ToString()}' to {configPath}");
    }

    public void OnDispose() {
        SaveConfig();
        _layoutManager.UnsubscribeFromNodeActions();

        foreach (StatusTimerNode<TKey> node in _layoutManager.AllNodes)
        {
            if (node != null) {
                GlobalServices.NativeController.DetachNode(node);
                node.Dispose();
            }
        }

        if (_layoutManager.RootContainer != null) {
            GlobalServices.NativeController.DetachNode(_layoutManager.RootContainer);
            _layoutManager.RootContainer.Dispose();
        }
        if (_layoutManager.BackgroundNode != null) {
            GlobalServices.NativeController.DetachNode(_layoutManager.BackgroundNode);
            _layoutManager.BackgroundNode.Dispose();
        }

        _isSetupCompleted = false;
    }

    private void AddLabelTimeLine(NodeBase node) {
        // Future Zeff, this always goes on a parent
        Timeline labels = new TimelineBuilder()
            .BeginFrameSet(1, 20)
            .AddLabel(1, 10, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(20, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build();

        node.AddTimeline(labels);
    }
}
