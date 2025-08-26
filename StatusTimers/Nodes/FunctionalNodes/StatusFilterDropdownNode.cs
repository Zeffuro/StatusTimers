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
    private readonly Func<List<LuminaStatus>> _optionsProvider;
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private StatusFilterListNode? _statusListNode;
    private readonly Action? _onChanged;
    private readonly bool _allowNoResult;
    private bool _isUpdating = false;

    private IconImageNode? _iconNode;
    private LuminaStatus? _currentSelection;
    private TextInputNode _textInputNode;
    private TextDropDownNode? _dropdownNode;
    private TextButtonNode? _addButtonNode;

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
            Width = 200,
            Height = 28,
            OnInputComplete = input => {
                string filter = input.TextValue ?? "";
                UpdateDropdownOptions(filter);
            }
        };
        AddNode(_textInputNode);

        AddDummy(120);

        _iconNode = new IconImageNode
        {
            Size = new System.Numerics.Vector2(24, 32),
            IsVisible = true,
            IconId = 0
        };
        AddNode(_iconNode);

        UpdateDropdownOptions(string.Empty);
    }

    private void UpdateDropdownOptions(string filter)
    {
        if (_isUpdating) {
            return;
        }

        _isUpdating = true;

        var toRemove = Nodes.Where(n => n is TextDropDownNode || n is TextButtonNode).ToList();
        foreach (var node in toRemove) {
            RemoveNode(node);
        }

        var allOptions = _optionsProvider();
        var filtered = allOptions
            .Where(opt =>
                string.IsNullOrEmpty(filter) ||
                $"{opt.RowId} {opt.Name.ExtractText()}".Contains(filter, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var displayOptions = (filtered.Count == 0 && _allowNoResult)
            ? new List<string> { "No results found" }
            : filtered.Select(opt => $"{opt.RowId} {opt.Name.ExtractText()}").ToList();

        _dropdownNode = new TextDropDownNode
        {
            IsVisible = true,
            Y = 2,
            Width = 200,
            Height = 28,
            MaxListOptions = 5,
            Options = displayOptions,
            OnOptionSelected = s => SetIcon(s, filtered),
            SelectedOption = displayOptions.FirstOrDefault() ?? "No results found"
        };
        AddNode(_dropdownNode);

        _addButtonNode = new TextButtonNode
        {
            IsVisible = true,
            Y = 2,
            Height = 28,
            Width = 32,
            Label = "+",
            OnClick = OnAddButtonClick
        };
        AddNode(_addButtonNode);

        if (displayOptions.Count > 0 && displayOptions[0] != "No results found") {
            SetIcon(displayOptions[0], filtered);
        } else {
            SetIcon("", filtered);
        }
        _isUpdating = false;
    }

    private void OnAddButtonClick()
    {
        if (_currentSelection.HasValue)
        {
            var statusId = _currentSelection.Value.RowId;
            if (_getConfig().FilterList.Add(statusId))
            {
                _statusListNode?.Refresh();
                _onChanged?.Invoke();
            }
        }
    }

    private void SetIcon(string selectedOption, List<LuminaStatus> filtered)
    {
        var idx = filtered.FindIndex(opt => $"{opt.RowId} {opt.Name.ExtractText()}" == selectedOption);
        _currentSelection = idx >= 0 ? filtered[idx] : null;
        if (_iconNode != null && _currentSelection.HasValue) {
            _iconNode.IconId = _currentSelection.Value.Icon;
        } else if (_iconNode != null) {
            _iconNode.IconId = 230402;
        }
    }

    public void SetStatusListNode(StatusFilterListNode listNode) {
        _statusListNode = listNode;
    }
}
