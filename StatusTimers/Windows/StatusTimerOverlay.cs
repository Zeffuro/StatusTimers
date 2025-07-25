using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.NodeParts;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using Newtonsoft.Json;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Layout;
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
public abstract class StatusTimerOverlay<TKey> : SimpleComponentNode {
    private bool isDisposed = false;

    private NodeKind _nodeKind;

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
        OverlayConfig = new StatusTimerOverlayConfig(_nodeKind);

        _layoutManager = new StatusOverlayLayoutManager<TKey>(
            this,
            () => OverlayConfig
        );

        _layoutManager.SetNodeActionHandler(HandleStatusNodeAction);

        _dataSourceManager = new StatusDataSourceManager<TKey>(
            _source,
            _nodeKind,
            () => IsPreviewEnabled,
            () => OverlayConfig.ShowPermaIcons,
            () => OverlayConfig.MaxStatuses,
            () => OverlayConfig.ItemsPerLine,
            () => OverlayConfig.PrimarySort,
            () => OverlayConfig.PrimarySortOrder,
            () => OverlayConfig.SecondarySort,
            () => OverlayConfig.SecondarySortOrder,
            () => OverlayConfig.TertiarySort,
            () => OverlayConfig.TertiarySortOrder
        );

        OverlayConfig.OnPropertyChanged += (property, updateNodes) => {
            GlobalServices.Logger.Verbose($"Property changed: {property}");
            OnPropertyChanged(property, updateNodes);
        };
    }

    public Vector2 CalculatedOverlaySize => _layoutManager.CalculatedOverlaySize;

    public override Vector4 Color { get; set; }
    public override Vector3 AddColor { get; set; }
    public override Vector3 MultiplyColor { get; set; }

    public bool IsLocked {
        get;
        set {
            if (field == value) {
                return;
            }

            field = value;
            _layoutManager.ToggleBackground(value);
            if (_isSetupCompleted && !_isConfigLoading) {
                GlobalServices.Framework.RunOnFrameworkThread(() => ToggleDrag(IsLocked));
            }
        }
    } = true;

    public bool IsPreviewEnabled { get; set; }

    [JsonProperty]
    public StatusTimerOverlayConfig? OverlayConfig { get; set; }

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

        Scale = new Vector2(OverlayConfig.ScaleInt * 0.01f);

        OnMoveComplete = SaveConfig;
        _isSetupCompleted = true;
    }

    public void OnUpdate() {
        List<StatusInfo> finalSortedList = _dataSourceManager.FetchAndProcessStatuses(OverlayConfig);

        _layoutManager.UpdateNodeContent(finalSortedList, _nodeKind);

        Active.Clear();
        foreach (StatusTimerNode<TKey> node in _layoutManager.AllNodes) {
            if (node.IsVisible && node.StatusInfo.Id != 0) {
                Active[_dataSourceManager.KeyOf(node.StatusInfo)] = node;
            }
        }

        _layoutManager.RecalculateLayout();
    }

    private void ToggleDrag(bool isLocked) {
        if (!ThreadSafety.IsMainThread) {
            return;
        }
        if (!isLocked) {
            EnableEditMode(NodeEditMode.Move);
        }
        else {
            DisableEditMode(NodeEditMode.Move);
        }

        _layoutManager.ToggleBackground(isLocked);
    }

    private void HandleStatusNodeAction(uint statusId, ulong gameObjectToTargetId, NodeKind nodeKind,
        bool allowDismiss, bool allowTarget) {
        if (nodeKind == NodeKind.Combined && allowDismiss) {
            GameStatusManager.ExecuteStatusOff(statusId);
        }

        if (nodeKind == NodeKind.MultiDoT && allowTarget) {
            GlobalServices.TargetManager.Target =
                GlobalServices.ObjectTable.FirstOrDefault(gameObject =>
                    gameObject is not null && gameObject.GameObjectId == gameObjectToTargetId);
        }
    }

    private void OnPropertyChanged(string property, bool updateNodes = false) {
        if (!_isConfigLoading) {
            if (property == "ScaleInt") {
                Scale = new Vector2(OverlayConfig.ScaleInt * 0.01f);
            }

            if (property == "Enabled") {
                Helpers.Util.ApplyConfigProps(OverlayConfig, this);
            }

            if (updateNodes) {
                _layoutManager.UpdateAllNodesDisplay();
            }
        }
        SaveConfig();
    }

    private void SetSortDefaults(NodeKind nodeKind)
    {
        switch (nodeKind) {
            case NodeKind.MultiDoT:
                OverlayConfig.PrimarySort   = SortCriterion.EnemyLetter;
                OverlayConfig.SecondarySort = SortCriterion.TimeRemaining;
                OverlayConfig.TertiarySort  = SortCriterion.None;
                break;
            case NodeKind.Combined:
            default:
                OverlayConfig.PrimarySort   = SortCriterion.StatusType;
                OverlayConfig.SecondarySort = SortCriterion.OwnStatusFirst;
                OverlayConfig.TertiarySort  = SortCriterion.PartyPriority;
                break;
        }
    }

    public void LoadConfig() {
        string configPath = Path.Combine(GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");
        Load(configPath);

        if (OverlayConfig != null) {
            Helpers.Util.ApplyConfigProps(OverlayConfig, this);
        }

        GlobalServices.Logger.Info($"Loaded overlay '{_nodeKind.ToString()}' from {configPath}");
    }

    public void SaveConfig() {
        if (!_isSetupCompleted) {
            return;
        }

        string configPath = Path.Combine(GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind.ToString()}.json");

        if (OverlayConfig != null) {
            Helpers.Util.SaveOverlayProps(OverlayConfig, this);
        }

        Save(configPath);
        GlobalServices.Logger.Verbose($"Saved overlay '{_nodeKind.ToString()}' to {configPath}");
    }

    public void OnDispose() {
        if (isDisposed) {
            return;
        }
        isDisposed = true;

        SaveConfig();
        _layoutManager.UnsubscribeFromNodeActions();

        foreach (StatusTimerNode<TKey> node in _layoutManager.AllNodes)
        {
            if (node != null) {
                GlobalServices.NativeController.DetachNode(node);
            }
        }

        if (_layoutManager.RootContainer != null) {
            GlobalServices.NativeController.DetachNode(_layoutManager.RootContainer);
            _layoutManager.SetNodeNull(false);
        }
        if (_layoutManager.BackgroundNode != null) {
            GlobalServices.NativeController.DetachNode(_layoutManager.BackgroundNode);
            _layoutManager.SetNodeNull(true);
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
