using Dalamud.Utility;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using Newtonsoft.Json;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Logic;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;
using Util = StatusTimers.Helpers.Util;

namespace StatusTimers.Windows;

[JsonObject(MemberSerialization.OptIn)]
public class StatusTimerOverlayNode<TKey> : OverlayNode where TKey : notnull {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly NodeKind _nodeKind;
    private readonly SimpleOverlayNode _rootContainer;
    private readonly HUDLayoutBackgroundNode _backgroundNode;
    private readonly HybridDirectionalFlexNode _statusContainer;

    private bool _isInitialized;
    private bool _suppressConfigEvents;
    private bool _isLocked = true;
    private bool _isPreviewEnabled;
    private Func<IReadOnlyList<StatusInfo>>? _statusProvider;

    private StatusTimerNode<TKey>.StatusNodeActionHandler? _nodeActionHandler;

    [JsonProperty]
    public StatusTimerOverlayConfig OverlayConfig {
        get;
        private set;
    }

    public Vector2 CalculatedOverlaySize { get; private set; }

    public bool IsLocked {
        get => _isLocked;
        set {
            if (_isLocked == value) {
                return;
            }

            _isLocked = value;
            GlobalServices.Framework.RunOnFrameworkThread(() => {
                if (!_isLocked) {
                    EnableEditMode(NodeEditMode.Move);
                }
                else {
                    DisableEditMode(NodeEditMode.Move);
                }
            });
            UpdateBackgroundVisibility();
        }
    }

    public bool IsPreviewEnabled {
        get => _isPreviewEnabled;
        set {
            if (_isPreviewEnabled == value) {
                return;
            }
            _isPreviewEnabled = value;
        }
    }

    public StatusTimerOverlayNode(NodeKind nodeKind, StatusTimerOverlayConfig? config = null) {
        _nodeKind = nodeKind;
        OverlayConfig = config ?? new StatusTimerOverlayConfig(nodeKind);
        OverlayConfigRegistry.Register(nodeKind, OverlayConfig);
        OverlayConfig.OnPropertyChanged += HandleConfigPropertyChanged;

        _rootContainer = new SimpleOverlayNode() {
            IsVisible = true
        };
        _rootContainer.AttachNode(this);

        _backgroundNode = new HUDLayoutBackgroundNode {
            IsVisible = false
        };
        _backgroundNode.AttachNode(_rootContainer);

        _statusContainer = new HybridDirectionalFlexNode {
            IsVisible = true,
            DisableCollisionNode = true
        };
        _statusContainer.AttachNode(_rootContainer);

        OnMoveComplete = _ => SaveConfig();
    }

    public void Initialize() {
        if (_isInitialized) {
            return;
        }

        _suppressConfigEvents = true;
        LoadConfig();
        _suppressConfigEvents = false;

        ApplyLayoutSettings();
        Scale = new Vector2(OverlayConfig.ScaleInt * 0.01f);
        Util.ApplyConfigProps(OverlayConfig, this);
        UpdateBackgroundVisibility();

        _isInitialized = true;
    }

    protected override void OnUpdate() {
        base.Update();
        Initialize();
        if (_statusProvider != null) {
            UpdateStatuses(_statusProvider());
        }
        else {
            _statusContainer.RecalculateLayout();
        }
    }

    public void SetNodeActionHandler(StatusTimerNode<TKey>.StatusNodeActionHandler? handler) {
        _nodeActionHandler = handler;
        foreach (var node in _statusContainer.GetNodes<StatusTimerNode<TKey>>()) {
            node.OnStatusNodeActionTriggered -= handler;
            if (handler != null) {
                node.OnStatusNodeActionTriggered += handler;
            }
        }
    }

    public void SetStatusProvider(Func<IReadOnlyList<StatusInfo>> provider) {
        _statusProvider = provider;
    }

    public void UpdateStatuses(IReadOnlyList<StatusInfo> statuses) {
        Initialize();

        _statusContainer.SyncWithListData(
            statuses,
            node => node.StatusInfo,
            CreateStatusNode);

        foreach (var node in _statusContainer.GetNodes<StatusTimerNode<TKey>>()) {
            var updated = statuses.FirstOrDefault(status =>
                status.Id == node.StatusInfo.Id &&
                status.GameObjectId == node.StatusInfo.GameObjectId &&
                status.IconId == node.StatusInfo.IconId);
            if (updated != null) {
                node.StatusInfo = updated;
            }
        }

        SortStatusNodes();
        _statusContainer.RecalculateLayout();
    }

    public void UpdateAllNodesDisplay(string changedProperty) {
        foreach (var node in _statusContainer.GetNodes<StatusTimerNode<TKey>>()) {
            node.ApplyOverlayConfig(changedProperty);
        }
    }

    public void ToggleBackground(bool show) {
        _backgroundNode.IsVisible = show;
    }

    private StatusTimerNode<TKey> CreateStatusNode(StatusInfo info) {
        var node = new StatusTimerNode<TKey>(() => OverlayConfig) {
            Width = OverlayConfig.RowWidth,
            Height = OverlayConfig.RowHeight,
            Kind = _nodeKind,
            StatusInfo = info,
            IsVisible = true
        };

        if (_nodeActionHandler != null) {
            node.OnStatusNodeActionTriggered += _nodeActionHandler;
        }

        return node;
    }

    private void SortStatusNodes() {
        var comparison = StatusSorter.GetNodeComparison<TKey>(
            OverlayConfig.PrimarySort,
            OverlayConfig.PrimarySortOrder,
            OverlayConfig.SecondarySort,
            OverlayConfig.SecondarySortOrder,
            OverlayConfig.TertiarySort,
            OverlayConfig.TertiarySortOrder);

        var nodes = _statusContainer.GetNodes<StatusTimerNode<TKey>>().ToList();
        var sorted = nodes
            .OrderBy(node => node, Comparer<StatusTimerNode<TKey>>.Create(comparison))
            .ToList();

        if (!nodes.SequenceEqual(sorted)) {
            _statusContainer.ReorderNodes((a, b) =>
                comparison((StatusTimerNode<TKey>)a, (StatusTimerNode<TKey>)b));
        }
    }

    private void HandleConfigPropertyChanged(string propertyName, bool updateNodes) {
        if (_suppressConfigEvents) {
            return;
        }

        if (propertyName == nameof(StatusTimerOverlayConfig.ScaleInt)) {
            Scale = new Vector2(OverlayConfig.ScaleInt * 0.01f);
        }

        if (propertyName == nameof(StatusTimerOverlayConfig.Enabled) ||
            propertyName == nameof(StatusTimerOverlayConfig.Position)) {
            Util.ApplyConfigProps(OverlayConfig, this);
        }

        if (propertyName is nameof(StatusTimerOverlayConfig.RowWidth) or
            nameof(StatusTimerOverlayConfig.RowHeight) or
            nameof(StatusTimerOverlayConfig.ItemsPerLine) or
            nameof(StatusTimerOverlayConfig.MaxStatuses) or
            nameof(StatusTimerOverlayConfig.StatusHorizontalPadding) or
            nameof(StatusTimerOverlayConfig.StatusVerticalPadding) or
            nameof(StatusTimerOverlayConfig.GrowDirection) or
            nameof(StatusTimerOverlayConfig.FillRowsFirst)) {
            ApplyLayoutSettings();
        }

        if (updateNodes) {
            UpdateAllNodesDisplay(propertyName);
        }

        SaveConfig();
    }

    private void ApplyLayoutSettings() {
        CalculatedOverlaySize = OverlayLayoutHelper.CalculateOverlaySize(OverlayConfig);

        _rootContainer.Size = CalculatedOverlaySize;
        Size = CalculatedOverlaySize;
        _backgroundNode.Size = CalculatedOverlaySize;

        _statusContainer.Width = CalculatedOverlaySize.X;
        _statusContainer.Height = CalculatedOverlaySize.Y;
        _statusContainer.HorizontalPadding = OverlayConfig.StatusHorizontalPadding;
        _statusContainer.VerticalPadding = OverlayConfig.StatusVerticalPadding;
        _statusContainer.ItemsPerLine = Math.Max(1, OverlayConfig.ItemsPerLine);
        _statusContainer.FillRowsFirst = OverlayConfig.FillRowsFirst;
        _statusContainer.GrowDirection = (FlexGrowDirection)OverlayConfig.GrowDirection;

        foreach (var node in _statusContainer.GetNodes<StatusTimerNode<TKey>>()) {
            node.Width = OverlayConfig.RowWidth;
            node.Height = OverlayConfig.RowHeight;
        }
    }

    private void UpdateBackgroundVisibility() {
        _backgroundNode.IsVisible = !_isLocked;
    }

    private void LoadConfig() {
        string configPath = Path.Combine(
            GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind}.json");

        if (!File.Exists(configPath)) {
            return;
        }

        try {
            string json = File.ReadAllText(configPath);
            var loaded = JsonConvert.DeserializeObject<StatusTimerOverlayConfig>(json);
            if (loaded == null) {
                return;
            }

            OverlayConfig.OnPropertyChanged -= HandleConfigPropertyChanged;
            OverlayConfig = loaded;
            OverlayConfig.OnPropertyChanged += HandleConfigPropertyChanged;
            OverlayConfigRegistry.Register(_nodeKind, OverlayConfig);

            StatusTimerOverlayConfigHelper.MigrateLegacyConfig(OverlayConfig);
            Util.ApplyConfigProps(OverlayConfig, this);
        }
        catch (Exception ex) {
            GlobalServices.Logger.Error($"Failed to load overlay '{_nodeKind}': {ex.Message}");
        }
    }

    private void SaveConfig() {
        if (!_isInitialized) {
            return;
        }

        string configPath = Path.Combine(
            GlobalServices.PluginInterface.GetPluginConfigDirectory(),
            $"{_nodeKind}.json");

        try {
            Util.SaveOverlayProps(OverlayConfig, this);
            string json = JsonConvert.SerializeObject(OverlayConfig, Formatting.Indented);
            File.WriteAllText(configPath, json);
            GlobalServices.Logger.Verbose($"Saved overlay '{_nodeKind}' to {configPath}");
        }
        catch (Exception ex) {
            GlobalServices.Logger.Error($"Failed to save overlay '{_nodeKind}': {ex.Message}");
        }
    }
}
