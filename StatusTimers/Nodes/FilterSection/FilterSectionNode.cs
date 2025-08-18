using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using StatusTimers.Config;
using StatusTimers.Nodes.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = System.Action;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.FilterSection;

public sealed class FilterSectionNode : VerticalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly List<Status> _statusList;
    private readonly Action? _onChanged;
    private readonly StatusFilterDropdownNode<Status> _dropdownNode;
    private readonly StatusFilterListNode _listNode;
    private readonly SectionHeaderNode _sectionHeaderNode;
    private readonly CheckboxOptionNode _sectionEnabledOptionNode;
    private readonly StatusFilterButtonGroupNode _filterButtonGroupNode;

    public FilterSectionNode(Func<StatusTimerOverlayConfig> getConfig, Action? onChanged = null) {
        _statusList = GlobalServices.DataManager.GetExcelSheet<LuminaStatus>()
            .Where(status => status.RowId != 0).ToList();
        _getConfig = getConfig;
        _onChanged = onChanged;

        _sectionHeaderNode = new SectionHeaderNode("Filter Settings");
        AddNode(_sectionHeaderNode);

        _sectionEnabledOptionNode = new CheckboxOptionNode("Enabled") {
            IsChecked = getConfig().FilterEnabled,
            OnClick = isChecked => {
                getConfig().FilterEnabled = isChecked;
                ToggleVisibility(isChecked);
                if (_listNode != null) {
                    _listNode.IsVisible = isChecked;
                    _listNode.Height = isChecked ? 400 : 16;
                    _listNode.FitContents = isChecked;
                    _listNode.RecalculateLayout();
                }

                onChanged?.Invoke();
            }
        };
        AddNode(_sectionEnabledOptionNode);

        AddDummy(16);

        _filterButtonGroupNode = new StatusFilterButtonGroupNode(_getConfig) {
            X = 18,
            IsVisible = true,
            Width = 600,
            Height = 60,
        };
        AddNode(_filterButtonGroupNode);

        _listNode = new StatusFilterListNode(_statusList, getConfig().FilterList, OnFilterChanged) {
            Height = 100,
            Width = 300,
            IsVisible = true,
            ItemSpacing = 4,
            FitContents = true,
        };
        _dropdownNode = new StatusFilterDropdownNode<Status>(
            () => _statusList,
            status => $"{status.RowId} {status.Name.ExtractText()}",
            status => status.Icon,
            getConfig,
            null,
            onChanged: OnFilterChanged
        ) {
            X = 16,
            Width = 600,
            Height = 60,
            IsVisible = true
        };
        _dropdownNode.SetStatusListNode(_listNode);

        AddNode(_dropdownNode);
        AddNode(_listNode);
    }

    private void ToggleVisibility(bool isVisible) {
        _listNode.IsVisible = isVisible;
        _dropdownNode.IsVisible = isVisible;
        _filterButtonGroupNode.IsVisible = isVisible;
        Height = isVisible ? 400 : 30;
        FitContents = isVisible;
    }

    private void OnFilterChanged() {
        _listNode.Refresh();
        _onChanged?.Invoke();
    }

    public void Refresh() {
        _listNode.Refresh();
    }
}
