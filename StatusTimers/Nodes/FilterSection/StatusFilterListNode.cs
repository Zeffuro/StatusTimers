using KamiToolKit.Nodes;
using StatusTimers.Config;
using System;
using System.Collections.Generic;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Nodes.FilterSection;

public sealed class StatusFilterListNode : VerticalListNode {
    private readonly List<LuminaStatus> _statusList;
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly Action? _onChanged;

    private bool _isRefreshing = false;
    private bool _isRemoving = false;

    public StatusFilterListNode(List<LuminaStatus> statusList, Func<StatusTimerOverlayConfig> getConfig, Action? onChanged = null) {
        Services.Services.Logger.Info("ListNode constructed");
        _statusList = statusList;
        _getConfig = getConfig;
        _onChanged = onChanged;

        foreach (var status in statusList) {
            AddStatusRow(status);
        }

        RecalculateLayout();
    }

    private void AddStatusRow(LuminaStatus status) {
        if (_getConfig().FilterList.Contains(status.RowId)) {
            AddNode(new StatusFilterRowNode(status, () => RemoveStatus(status.RowId)) {
                X = 18,
                Height = 32,
                Width = 320,
                IsVisible = true,
                ItemSpacing = 10
            });
        }
    }

    public void RemoveStatus(uint statusId) {
        if (_isRefreshing || _isRemoving) {
            return;
        }

        _isRemoving = true;
        if (_getConfig().FilterList.Remove(statusId)) {
            Refresh();
        };
        _isRemoving = false;
    }

    public void Refresh() {
        Services.Services.Logger.Info("ListNode.Refresh called");
        if (_isRefreshing) {
            return;
        }

        _isRefreshing = true;
        Clear();
        foreach (var status in _statusList) {
            AddStatusRow(status);
        }
        RecalculateLayout();
        _isRefreshing = false;
    }
}
