using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Windows;
using Lumina.Excel.Sheets;
using StatusTimers.Helpers;
using Action = System.Action;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public static class FilterUIFactory
{
    // Delegate for when something in the filter section changes
    public delegate void FilterChangedCallback();

    public static VerticalListNode<NodeBase> CreateFilterSection(
        StatusTimerOverlayConfig? config,
        FilterChangedCallback? onChanged = null)
    {
        var section = new VerticalListNode<NodeBase>
        {
            IsVisible = true,
            Width = 600,
            Height = 420,
            ItemVerticalSpacing = 4
        };

        section.AddNode(new TextNode
        {
            IsVisible = true,
            Width = 120,
            Height = TextStyles.Header.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.Header.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = "Filter Settings"
        });

        var allStatuses = GlobalServices.DataManager.GetExcelSheet<LuminaStatus>()
            .Where(status => status.RowId != 0).ToList();

        section.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
            "Enabled",
            () => config.FilterEnabled,
            isChecked => { config.FilterEnabled = isChecked; onChanged?.Invoke(); }
        ));

        section.AddDummy(new ResNode(), ConfigurationUIFactory.CheckBoxHeight);

        if (config.FilterEnabled)
        {
            var radioButtonGroup = new RadioButtonGroupNode
            {
                X = ConfigurationUIFactory.OptionOffset,
                Width = 200,
                Height = TextStyles.Header.Height,
                IsVisible = true,
            };

            radioButtonGroup.AddButton("Blacklist", () =>
            {
                config.FilterIsBlacklist = true;
                onChanged?.Invoke();
            });
            radioButtonGroup.AddButton("Whitelist", () =>
            {
                config.FilterIsBlacklist = false;
                onChanged?.Invoke();
            });

            section.AddNode(radioButtonGroup);
            radioButtonGroup.SelectedOption = config.FilterIsBlacklist ? "Blacklist" : "Whitelist";

            var filteredDropdownNode = CreateFilteredDropdown(
                () => allStatuses,
                status => $"{status.RowId} {status.Name.ExtractText()}",
                status => status.Icon,
                status =>
                {
                    if (config.FilterList.Add(status.RowId)) {
                        onChanged?.Invoke();
                    }
                });

            section.AddNode(filteredDropdownNode);

            var statusListNode = CreateStatusListNode(
                config.FilterList,
                allStatuses,
                rowId =>
                {
                    if (config.FilterList.Remove(rowId)) {
                        onChanged?.Invoke();
                    }
                }
            );
            section.AddNode(statusListNode);
        }

        return section;
    }

    public static HorizontalListNode<NodeBase> CreateFilteredDropdown<T>(
        Func<List<T>> optionsProvider,
        Func<T, string> displaySelector,
        Func<T, uint>? iconSelector,
        Action<T?> setter,
        bool allowNoResult = true)
    {
        var flexNode = new HorizontalListNode<NodeBase>
        {
            IsVisible = true,
            X = ConfigurationUIFactory.OptionOffset,
            Width = 600,
            Height = 60
        };

        TextDropDownNode? textDropDown = null;
        IconImageNode? iconNode = null;
        T? currentSelection = default;

        var textInput = new TextInputNode
        {
            IsVisible = true,
            Width = 200,
            Height = 28
        };

        var textButton = new TextButtonNode
        {
            IsVisible = true,
            Y = 2,
            Height = 28,
            Width = 32,
            Label = "+",
            OnClick = () => setter(currentSelection)
        };

        void ReplaceDropDown(List<T> filtered)
        {
            if (textDropDown != null) {
                flexNode.RemoveNode(textDropDown);
            }

            if (textButton != null) {
                flexNode.RemoveNode(textButton);
            }

            var displayOptions = (filtered.Count == 0 && allowNoResult)
                ? new List<string> { "No results found" }
                : filtered.Select(displaySelector).ToList();

            textDropDown = new TextDropDownNode
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
                    if (s == "No results found") {
                        return;
                    }
                },
                SelectedOption = displayOptions.FirstOrDefault() ?? "No results found"
            };
            flexNode.AddNode(textDropDown);
            flexNode.AddNode(textButton);

            if (displayOptions.Count > 0 && displayOptions[0] != "No results found") {
                SetIcon(displayOptions[0], filtered);
            }
            else {
                SetIcon("", filtered);
            }
        }

        void SetIcon(string selectedOption, List<T> filtered)
        {
            var idx = filtered.FindIndex(opt => displaySelector(opt) == selectedOption);
            currentSelection = idx >= 0 ? filtered[idx] : default;
            if (iconNode != null && currentSelection != null && iconSelector != null) {
                iconNode.IconId = iconSelector(currentSelection);
            }
            else if (iconNode != null) {
                iconNode.IconId = 230402;
            }
        }

        void UpdateDropdownOptions(string filter)
        {
            var allOptions = optionsProvider();
            var filtered = allOptions
                .Where(opt =>
                    string.IsNullOrEmpty(filter) ||
                    (opt is LuminaStatus s &&
                        (s.RowId.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                         s.Name.ExtractText()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true)
                    )
                )
                .ToList();
            ReplaceDropDown(filtered);
        }

        textInput.OnInputComplete = input =>
        {
            string filter = input.TextValue ?? "";
            GlobalServices.Framework.RunOnTick(() => UpdateDropdownOptions(filter), delayTicks: 1);
        };

        flexNode.AddNode(textInput);
        flexNode.AddDummy(new ResNode(), 120);

        if (iconSelector != null)
        {
            iconNode = new IconImageNode
            {
                Size = new System.Numerics.Vector2(24, 32),
                IsVisible = true,
                IconId = 0
            };
            flexNode.AddNode(iconNode);
        }

        UpdateDropdownOptions(string.Empty);

        return flexNode;
    }

    public static VerticalListNode<NodeBase> CreateStatusListNode(
        HashSet<uint> filterList,
        List<LuminaStatus> allStatuses,
        Action<uint> onRemove = null)
    {
        var statusListNode = new VerticalListNode<NodeBase>
        {
            Height = 200,
            Width = 300,
            IsVisible = true,
            ItemVerticalSpacing = 4
        };

        foreach (var rowId in filterList.ToList().Order())
        {
            var status = allStatuses.FirstOrDefault(s => s.RowId == rowId);

            var row = new HorizontalListNode<NodeBase>
            {
                X = ConfigurationUIFactory.OptionOffset,
                Height = 32,
                Width = 300,
                IsVisible = true,
                ItemHorizontalSpacing = 4
            };

            row.AddNode(new IconImageNode
            {
                Y = -4,
                Size = new System.Numerics.Vector2(24, 32),
                IsVisible = true,
                IconId = status.Icon
            });

            row.AddNode(new TextNode
            {
                Text = status.RowId.ToString() ?? rowId.ToString(),
                IsVisible = true,
                Height = 24,
                Width = 40,
                AlignmentType = AlignmentType.Right
            });

            row.AddNode(new TextNode
            {
                Text = status.Name.ExtractText() ?? "",
                IsVisible = true,
                Height = 24,
                Width = 180,
                AlignmentType = AlignmentType.Left
            });

            row.AddNode(new TextButtonNode
            {
                Label = "-",
                Width = 32,
                Height = 28,
                IsVisible = true,
                OnClick = () => onRemove?.Invoke(rowId)
            });

            statusListNode.AddNode(row);
        }

        return statusListNode;
    }
}
