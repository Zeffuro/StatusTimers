using KamiToolKit.Nodes;
using StatusTimers.Nodes;
using StatusTimers.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public class StatusFilterDropdownNode<T> : HorizontalListNode
{
    private readonly Func<List<T>> _optionsProvider;
    private readonly Func<T, string> _displaySelector;
    private readonly Func<T, uint>? _iconSelector;
    private readonly HashSet<uint> _filterList;
    private readonly StatusFilterListNode _statusListNode;
    private readonly Action? _onChanged;
    private readonly bool _allowNoResult;
    private bool _isUpdating = false;

    private IconImageNode? _iconNode;
    private T? _currentSelection;

    public StatusFilterDropdownNode(
        Func<List<T>> optionsProvider,
        Func<T, string> displaySelector,
        Func<T, uint>? iconSelector,
        HashSet<uint> filterList,
        StatusFilterListNode statusListNode,
        Action? onChanged = null,
        bool allowNoResult = true)
    {
        Services.Logger.Info("DropdownNode constructed");
        _optionsProvider = optionsProvider;
        _displaySelector = displaySelector;
        _iconSelector = iconSelector;
        _filterList = filterList;
        _statusListNode = statusListNode;
        _onChanged = onChanged;
        _allowNoResult = allowNoResult;

        Width = 600;
        Height = 60;
        IsVisible = true;

        BuildNodes();
    }

    private void BuildNodes()
    {
        var textInput = new TextInputNode
        {
            IsVisible = true,
            Width = 200,
            Height = 28
        };

        textInput.OnInputComplete = input =>
        {
            string filter = input.TextValue ?? "";
            UpdateDropdownOptions(filter);
        };

        AddNode(textInput);
        AddDummy(120);

        if (_iconSelector != null)
        {
            _iconNode = new IconImageNode
            {
                Size = new System.Numerics.Vector2(24, 32),
                IsVisible = true,
                IconId = 0
            };
            AddNode(_iconNode);
        }

        UpdateDropdownOptions(string.Empty);
    }

    private void UpdateDropdownOptions(string filter)
    {
        Services.Logger.Info("DropdownNode.UpdateDropdownOptions called");
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
                _displaySelector(opt).Contains(filter, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var displayOptions = (filtered.Count == 0 && _allowNoResult)
            ? ["No results found"]
            : filtered.Select(_displaySelector).ToList();

        var dropdown = new TextDropDownNode
        {
            IsVisible = true,
            Y = 2,
            Width = 200,
            Height = 28,
            MaxListOptions = 5,
            Options = displayOptions,
            OnOptionSelected = s =>
            {
                SetIcon(s, filtered);
            },
            SelectedOption = displayOptions.FirstOrDefault() ?? "No results found"
        };
        AddNode(dropdown);

        var addButton = new TextButtonNode
        {
            IsVisible = true,
            Y = 2,
            Height = 28,
            Width = 32,
            Label = "+",
            OnClick = () => {
                if (_currentSelection != null && _iconSelector != null) {
                    var id = _iconSelector(_currentSelection);
                    if (_filterList.Add(id)) {
                        //_onChanged?.Invoke();
                    }
                }
            }
        };
        AddNode(addButton);

        if (displayOptions.Count > 0 && displayOptions[0] != "No results found") {
            SetIcon(displayOptions[0], filtered);
        }
        else {
            SetIcon("", filtered);
        }
        _isUpdating = false;
    }

    private void SetIcon(string selectedOption, List<T> filtered)
    {
        var idx = filtered.FindIndex(opt => _displaySelector(opt) == selectedOption);
        _currentSelection = idx >= 0 ? filtered[idx] : default;
        if (_iconNode != null && _currentSelection != null && _iconSelector != null) {
            _iconNode.IconId = _iconSelector(_currentSelection);
        }
        else if (_iconNode != null) {
            _iconNode.IconId = 230402;
        }
    }
}
