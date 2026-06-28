using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using StatusTimers.Config;
using StatusTimers.Nodes.FunctionalNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Action = System.Action;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class FilterSectionNode : TabbedVerticalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private readonly List<Status> _statusList;
    private readonly Action? _onChanged;
    private readonly StatusFilterDropdownNode _dropdownNode;
    private readonly StatusFilterListNode _listNode;
    private readonly SectionHeaderNode _sectionHeaderNode;
    private readonly CheckboxOptionNode _sectionEnabledOptionNode;
    private readonly ResNode _sectionSpacerNode;
    private readonly StatusFilterButtonGroupNode _filterButtonGroupNode;

    public FilterSectionNode(Func<StatusTimerOverlayConfig> getConfig, Action onChanged) {
        FitContents = true;
        _statusList = GlobalServices.DataManager.GetExcelSheet<LuminaStatus>()
            .Where(status => status.RowId != 0).ToList();
        _getConfig = getConfig;
        _onChanged = onChanged;

        _sectionHeaderNode = new SectionHeaderNode("Filter Settings");
        AddNode(_sectionHeaderNode);
        AddTab(1);

        _sectionSpacerNode = new ResNode { Height = 16 };

        _sectionEnabledOptionNode = new CheckboxOptionNode {
            String = "Enabled",
            IsChecked = getConfig().FilterEnabled,
            OnClick = isChecked => {
                getConfig().FilterEnabled = isChecked;
                ToggleVisibility(isChecked);
                onChanged();
            }
        };

        _filterButtonGroupNode = new StatusFilterButtonGroupNode(_getConfig, OnFilterChanged) {
            X = 18,
            IsVisible = getConfig().FilterEnabled,
            Width = 600,
            Height = 60,
        };

        _listNode = new StatusFilterListNode(_statusList, getConfig, OnFilterChanged) {
            Height = 100,
            Width = 300,
            IsVisible = getConfig().FilterEnabled,
            ItemSpacing = 2,
        };
        _dropdownNode = new StatusFilterDropdownNode(
            () => _statusList,
            getConfig,
            _listNode,
            OnFilterChanged
        ) {
            X = 18,
            Width = 600,
            Height = 60,
            IsVisible = getConfig().FilterEnabled
        };
        _dropdownNode.SetStatusListNode(_listNode);

        AddNode([
            _sectionEnabledOptionNode,
            _sectionSpacerNode,
            _filterButtonGroupNode,
            _dropdownNode
        ]);
        AddTab(1);
        AddNode(_listNode);

        ToggleVisibility(getConfig().FilterEnabled);
    }

    private void ToggleVisibility(bool isVisible) {
        _listNode.IsVisible = isVisible;
        _dropdownNode.IsVisible = isVisible;
        _filterButtonGroupNode.IsVisible = isVisible;
        _sectionSpacerNode.IsVisible = isVisible;
        FitContents = true;
        _listNode.RecalculateLayout();
        RecalculateLayout();
    }

    private void OnFilterChanged() {
        _listNode.Refresh();
        _onChanged?.Invoke();
    }

    public void OnUpdate() {
        _listNode.OnUpdate();
    }

    public void RecalculateStatusList() {
        _listNode.RecalculateLayout();
        RecalculateLayout();
    }
}
