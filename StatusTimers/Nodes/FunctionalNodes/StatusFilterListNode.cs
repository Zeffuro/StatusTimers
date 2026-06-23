using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusFilterListNode : ConfigVerticalListNode {
    private readonly Dictionary<uint, LuminaStatus> _statusesById;
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly Action? _onChanged;

    private bool _isRefreshing;
    private bool _shouldRefresh;

    public StatusFilterListNode(List<LuminaStatus> statusList, Func<StatusTimerOverlayConfig> getConfig, Action? onChanged = null) {
        _statusesById = statusList.ToDictionary(status => status.RowId);
        _getConfig = getConfig;
        _onChanged = onChanged;

        Refresh();
    }

    private StatusFilterRowNode CreateStatusRow(LuminaStatus status) {
        return new StatusFilterRowNode(status) {
            X = 18,
            Height = 32,
            Width = 320,
            IsVisible = true,
            OnRemove = () => RemoveStatus(status.RowId),
            ItemSpacing = 20
        };
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
            _shouldRefresh = false;

            if (_onChanged is not null) {
                _onChanged.Invoke();
            } else {
                Refresh();
            }
        }
    }

    public unsafe void Refresh() {
        if (_isRefreshing) {
            return;
        }

        _isRefreshing = true;

        var existingRows = GetNodes<StatusFilterRowNode>().ToList();
        if (existingRows.Count > 0) {
            RemoveNode(existingRows);
        }

        var rows = new List<StatusFilterRowNode>();
        foreach (var statusId in _getConfig().FilterList.OrderBy(statusId => statusId)) {
            if (_statusesById.TryGetValue(statusId, out var status)) {
                rows.Add(CreateStatusRow(status));
            }
        }

        if (rows.Count > 0) {
            FitContents = true;
            AddNode(rows);
        } else {
            FitContents = false;
            Height = 0.0f;
        }

        _isRefreshing = false;
    }
}
