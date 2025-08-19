using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusFilterListNode : VerticalListNode {
    private readonly List<LuminaStatus> _statusList;
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly Action? _onChanged;

    private bool _isRefreshing = false;
    private bool _shouldRefresh = false;

    public StatusFilterListNode(List<LuminaStatus> statusList, Func<StatusTimerOverlayConfig> getConfig, Action? onChanged = null) {
        _statusList = statusList;
        _getConfig = getConfig;
        _onChanged = onChanged;

        Refresh();
    }

    private void AddStatusRow(LuminaStatus status) {
        if (_getConfig().FilterList.Contains(status.RowId)) {
            AddNode(new StatusFilterRowNode(status, () => RemoveStatus(status.RowId)) {
                NodeId = status.RowId,
                X = 18,
                Height = 32,
                Width = 320,
                IsVisible = true,
                ItemSpacing = 10
            });
        }
    }

    public void RemoveStatus(uint statusId) {
        if (_isRefreshing) {
            return;
        }
        if (_getConfig().FilterList.Contains(statusId) && _getConfig().FilterList.Remove(statusId)) {
            _shouldRefresh = true;
        }
    }

    public void OnUpdate() {
        if (_shouldRefresh && !_isRefreshing) {
            Refresh();
            _onChanged?.Invoke();
            _shouldRefresh = false;
        }
    }

    public unsafe void Refresh() {
        if (_isRefreshing) {
            return;
        }

        _isRefreshing = true;
        foreach (var node in GetNodes<StatusFilterRowNode>().ToList()) {
            RaptureAtkModule.Instance()->ClearFocus();
            AtkStage.Instance()->ClearFocus();
            node.RemoveButtonNodeOnClick();
            //var teste = (AtkUnitBase*)node.ComponentBase;

            RemoveNode(node);
        }
        foreach (var status in _statusList) {
            AddStatusRow(status);
        }
        RecalculateLayout();
        _isRefreshing = false;
    }
}
