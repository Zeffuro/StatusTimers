// StatusTimers/Nodes/FilterSection/StatusFilterDropdownNode.cs

using KamiToolKit.Nodes;
using StatusTimers.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusFilterDropdownNode : HorizontalListNode
{
    private const int MinimumSearchLength = 2;
    private const int MaxSearchResults = 50;

    private readonly Func<List<LuminaStatus>> _optionsProvider;
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private StatusFilterListNode? _statusListNode;
    private readonly Action? _onChanged;
    private readonly bool _allowNoResult;
    private bool _isUpdating;

    private IconImageNode? _iconNode;
    private LuminaStatus? _currentSelection;
    private List<LuminaStatus> _filteredOptions = [];
    private TextInputNode _textInputNode;
    private readonly StringDropDownNode _dropdownNode;
    private readonly TextButtonNode _addButtonNode;

    public StatusFilterDropdownNode(
        Func<List<LuminaStatus>> optionsProvider,
        Func<StatusTimerOverlayConfig> getConfig,
        StatusFilterListNode? statusListNode = null,
        Action? onChanged = null,
        bool allowNoResult = true)
    {
        _optionsProvider = optionsProvider;
        _getConfig = getConfig;
        _statusListNode = statusListNode;
        _onChanged = onChanged;
        _allowNoResult = allowNoResult;

        _textInputNode = new TextInputNode {
            IsVisible = true,
            Width = 250,
            Height = 28,
            OnInputComplete = input => {
                string filter = input.ToString();
                UpdateDropdownOptions(filter);
            }
        };

        _iconNode = new IconImageNode
        {
            Size = new System.Numerics.Vector2(24, 32),
            IsVisible = true,
            IconId = 0,
            FitTexture = true
        };

        _dropdownNode = new StringDropDownNode
        {
            IsVisible = true,
            Y = 2,
            Width = 250,
            Height = 28,
            MaxListOptions = 5,
            Options = [GetEmptySearchMessage()],
            OnOptionSelected = SetIcon,
            SelectedOption = GetEmptySearchMessage()
        };

        _addButtonNode = new TextButtonNode
        {
            IsVisible = true,
            Y = 2,
            Height = 28,
            Width = 32,
            String = "+",
            OnClick = OnAddButtonClick
        };

        AddNode([
            _textInputNode,
            new ResNode { Size = new System.Numerics.Vector2(10) },
            _iconNode,
            _dropdownNode,
            _addButtonNode
        ]);
    }

    private void UpdateDropdownOptions(string filter)
    {
        if (_isUpdating) {
            return;
        }

        _isUpdating = true;

        try {
            var normalizedFilter = filter.Trim();
            if (normalizedFilter.Length < MinimumSearchLength) {
                _filteredOptions = [];
                _dropdownNode.Options = [GetEmptySearchMessage()];
                _dropdownNode.SelectedOption = GetEmptySearchMessage();
                SetIcon(string.Empty);
                return;
            }

            var allOptions = _optionsProvider();
            _filteredOptions = allOptions
                .Where(opt =>
                    opt.RowId.ToString().Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) ||
                    opt.Name.ToString().Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)
                )
                .Take(MaxSearchResults)
                .ToList();

            var displayOptions = (_filteredOptions.Count == 0 && _allowNoResult)
                ? new List<string> { "No results found" }
                : _filteredOptions.Select(GetDisplayName).ToList();

            _dropdownNode.Options = displayOptions;
            _dropdownNode.SelectedOption = displayOptions.FirstOrDefault() ?? "No results found";
            if (displayOptions.Count > 0 && displayOptions[0] != "No results found") {
                SetIcon(displayOptions[0]);
            } else {
                SetIcon(string.Empty);
            }
        } finally {
            _isUpdating = false;
        }
    }

    private void OnAddButtonClick()
    {
        if (_currentSelection.HasValue)
        {
            var statusId = _currentSelection.Value.RowId;
            if (_getConfig().FilterList.Add(statusId))
            {
                if (_onChanged is not null) {
                    _onChanged.Invoke();
                } else {
                    _statusListNode?.Refresh();
                }
            }
        }
    }

    private void SetIcon(string selectedOption)
    {
        var selectedStatus = _filteredOptions.FirstOrDefault(opt => GetDisplayName(opt) == selectedOption);
        _currentSelection = selectedStatus.RowId != 0 ? selectedStatus : null;
        if (_iconNode != null && _currentSelection.HasValue) {
            _iconNode.IconId = _currentSelection.Value.Icon;
        } else if (_iconNode != null) {
            _iconNode.IconId = 230402;
        }
    }

    public void SetStatusListNode(StatusFilterListNode listNode) {
        _statusListNode = listNode;
    }

    private static string GetDisplayName(LuminaStatus status)
        => $"{status.RowId} {status.Name.ToString()}";

    private static string GetEmptySearchMessage()
        => $"Type {MinimumSearchLength}+ characters";
}
