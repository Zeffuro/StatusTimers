using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using Newtonsoft.Json;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Logic;
using StatusTimers.Models;
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
    private readonly ResNode _rootContainer;
    private readonly HybridDirectionalFlexNode<StatusTimerNode<TKey>> _statusContainer;
    private readonly Dictionary<StatusKey, StatusTimerNode<TKey>> _activeStatusNodes = new();
    private readonly List<StatusTimerNode<TKey>> _allStatusNodes = [];
    private readonly Queue<StatusTimerNode<TKey>> _inactiveStatusNodes = new();

    private bool _isInitialized;
    private bool _suppressConfigEvents;
    private bool _isLocked = true;

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
        }
    }

    public bool IsPreviewEnabled {
        get;
        set {
            if (field == value) {
                return;
            }

            field = value;
        }
    }

    public StatusTimerOverlayNode(NodeKind nodeKind, StatusTimerOverlayConfig? config = null) {
        _nodeKind = nodeKind;
        OverlayConfig = config ?? new StatusTimerOverlayConfig(nodeKind);
        OverlayConfigRegistry.Register(nodeKind, OverlayConfig);
        OverlayConfig.OnPropertyChanged += HandleConfigPropertyChanged;

        _rootContainer = new ResNode() {
            IsVisible = true
        };
        _rootContainer.AttachNode(this);

        _statusContainer = new HybridDirectionalFlexNode<StatusTimerNode<TKey>> {
            IsVisible = true
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

        _isInitialized = true;
    }

    protected override void OnUpdate() {
        Initialize();
        if (_statusProvider != null) {
            UpdateStatuses(_statusProvider());
        }
        else {
            _statusContainer.RecalculateLayout();
        }
    }

    public void SetNodeActionHandler(StatusTimerNode<TKey>.StatusNodeActionHandler? handler) {
        var previousHandler = _nodeActionHandler;
        _nodeActionHandler = handler;
        foreach (var node in _allStatusNodes) {
            if (previousHandler != null) {
                node.OnStatusNodeActionTriggered -= previousHandler;
            }
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

        var nodesChanged = ReconcileStatusNodes(statuses);
        var orderChanged = SortStatusNodes();

        if (nodesChanged && !orderChanged) {
            _statusContainer.RecalculateLayout();
        }
    }

    public void UpdateAllNodesDisplay(string changedProperty) {
        foreach (var node in _allStatusNodes) {
            node.ApplyOverlayConfig(changedProperty);
        }
    }

    private bool ReconcileStatusNodes(IReadOnlyList<StatusInfo> statuses) {
        var anythingChanged = false;
        var currentKeys = statuses
            .Select(status => status.Key)
            .ToHashSet();

        foreach (var (key, node) in _activeStatusNodes.ToList()) {
            if (currentKeys.Contains(key)) {
                continue;
            }

            _activeStatusNodes.Remove(key);
            node.ClearStatus();
            _inactiveStatusNodes.Enqueue(node);
            anythingChanged = true;
        }

        var nodesToAttach = new List<StatusTimerNode<TKey>>();
        foreach (var status in statuses) {
            var key = status.Key;
            if (_activeStatusNodes.TryGetValue(key, out var node)) {
                node.ActivateStatus(status);
                continue;
            }

            node = AcquireStatusNode(status, nodesToAttach);
            _activeStatusNodes[key] = node;
            anythingChanged = true;
        }

        if (nodesToAttach.Count != 0) {
            _statusContainer.AddNode(nodesToAttach);
        }

        return anythingChanged;
    }

    private StatusTimerNode<TKey> AcquireStatusNode(
        StatusInfo status,
        List<StatusTimerNode<TKey>> nodesToAttach) {
        if (_inactiveStatusNodes.TryDequeue(out var pooledNode)) {
            pooledNode.ActivateStatus(status);
            return pooledNode;
        }

        var node = CreateStatusNode(status);
        _allStatusNodes.Add(node);
        nodesToAttach.Add(node);
        return node;
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

    private bool SortStatusNodes() {
        var comparison = StatusSorter.GetNodeComparison<TKey>(
            OverlayConfig.PrimarySort,
            OverlayConfig.PrimarySortOrder,
            OverlayConfig.SecondarySort,
            OverlayConfig.SecondarySortOrder,
            OverlayConfig.TertiarySort,
            OverlayConfig.TertiarySortOrder);

        var nodes = _statusContainer.GetNodes<StatusTimerNode<TKey>>().ToList();
        if (nodes.Count < 2) {
            return false;
        }

        int CompareForLayout(StatusTimerNode<TKey> a, StatusTimerNode<TKey> b) {
            if (a.IsVisible != b.IsVisible) {
                return a.IsVisible ? -1 : 1;
            }

            if (!a.IsVisible) {
                return 0;
            }

            return comparison(a, b);
        }

        var sorted = nodes
            .OrderBy(node => node, Comparer<StatusTimerNode<TKey>>.Create(CompareForLayout))
            .ToList();

        if (nodes.SequenceEqual(sorted)) {
            return false;
        }

        _statusContainer.ReorderNodes((a, b) =>
            CompareForLayout((StatusTimerNode<TKey>)a, (StatusTimerNode<TKey>)b));

        return true;
    }

    private void HandleConfigPropertyChanged(string propertyName, bool updateNodes) {
        if (_suppressConfigEvents) {
            return;
        }

        GlobalServices.Framework.RunOnFrameworkThread(() => {
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
        });
    }

    private void ApplyLayoutSettings() {
        CalculatedOverlaySize = OverlayLayoutHelper.CalculateOverlaySize(OverlayConfig);

        _rootContainer.Size = CalculatedOverlaySize;
        Size = CalculatedOverlaySize;

        _statusContainer.Width = CalculatedOverlaySize.X;
        _statusContainer.Height = CalculatedOverlaySize.Y;
        _statusContainer.HorizontalPadding = OverlayConfig.StatusHorizontalPadding;
        _statusContainer.VerticalPadding = OverlayConfig.StatusVerticalPadding;
        _statusContainer.ItemsPerLine = Math.Max(1, OverlayConfig.ItemsPerLine);
        _statusContainer.FillRowsFirst = OverlayConfig.FillRowsFirst;
        _statusContainer.GrowDirection = (FlexGrowDirection)OverlayConfig.GrowDirection;

        foreach (var node in _allStatusNodes) {
            node.Width = OverlayConfig.RowWidth;
            node.Height = OverlayConfig.RowHeight;
        }
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
