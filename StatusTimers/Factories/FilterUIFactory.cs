using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
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
using System.Drawing;
using System.IO;
using System.Numerics;
using Action = System.Action;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public static class FilterUIFactory
{
    // Delegate for when something in the filter section changes
    public delegate void FilterChangedCallback();
    private const float CheckBoxHeight = 16;
    private const float SectionHeight = 400;

    public static VerticalListNode CreateFilterSection(
        Func<StatusTimerOverlayConfig> getConfig,
        FilterChangedCallback? onChanged = null,
        Action? onToggled = null)
    {
        var section = new VerticalListNode
        {
            IsVisible = true,
            Width = 600,
            Height = SectionHeight,
            ItemSpacing = 4,
            FitContents = true,
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
            AlignmentType = AlignmentType.Left,
            Text = "Filter Settings"
        });

        var allStatuses = GlobalServices.DataManager.GetExcelSheet<LuminaStatus>()
            .Where(status => status.RowId != 0).ToList();

        var filterContentGroup = new VerticalListNode
        {
            IsVisible = getConfig().FilterEnabled,
            FitContents = true,
            ItemSpacing = 4,
        };

        section.AddNode(ConfigurationUIFactory.CreateCheckboxOption(
            "Enabled",
            () => getConfig().FilterEnabled,
            isChecked => {
                getConfig().FilterEnabled = isChecked;
                if (filterContentGroup != null) {
                    filterContentGroup.IsVisible = isChecked;
                    section.Height = isChecked ? SectionHeight : CheckBoxHeight;
                    section.FitContents = isChecked;
                    section.RecalculateLayout();
                }

                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        ));

        section.AddDummy(new ResNode(), ConfigurationUIFactory.CheckBoxHeight);

        HorizontalListNode horizontalListNode = new() {
            X = ConfigurationUIFactory.OptionOffset,
            IsVisible = true,
            Width = 600,
            Height = 60,
        };

        var radioButtonGroup = new RadioButtonGroupNode
        {
            Width = 100,
            Height = 60,
            IsVisible = true,
        };

        radioButtonGroup.AddButton("Blacklist", () =>
        {
            getConfig().FilterIsBlacklist = true;
            onChanged?.Invoke();
        });
        radioButtonGroup.AddButton("Whitelist", () =>
        {
            getConfig().FilterIsBlacklist = false;
            onChanged?.Invoke();
        });

        radioButtonGroup.SelectedOption = getConfig().FilterIsBlacklist ? "Blacklist" : "Whitelist";
        horizontalListNode.AddNode(radioButtonGroup);

        horizontalListNode.AddNode(
            new ImGuiIconButtonNode {
                Height = 30,
                Width = 30,
                IsVisible = true,
                Tooltip = "Export Filter List",
                TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\upload.png"),
                OnClick = () => TryExportFilterListToClipboard(getConfig().FilterList)
            }
        );
        horizontalListNode.AddNode(
            new ImGuiIconButtonNode {
                X = 52,
                Height = 30,
                Width = 30,
                IsVisible = true,
                TooltipString = "     Import Filter List \n(hold shift to confirm)",
                TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\download.png"),
                OnClick = () => TryImportFilterListFromClipboard(getConfig(), onChanged)
            }
        );

        filterContentGroup.AddNode(horizontalListNode);

        var filteredDropdownNode = CreateFilteredDropdown(
            () => allStatuses,
            status => $"{status.RowId} {status.Name.ExtractText()}",
            status => status.Icon,
            status =>
            {
                if (getConfig().FilterList.Add(status.RowId)) {
                    onChanged?.Invoke();
                }
            });

        filterContentGroup.AddNode(filteredDropdownNode);

        var statusListNode = CreateStatusListNode(
            getConfig().FilterList,
            allStatuses,
            rowId =>
            {
                if (getConfig().FilterList.Remove(rowId)) {
                    onChanged?.Invoke();
                }
            }
        );
        filterContentGroup.AddNode(statusListNode);
        section.AddNode(filterContentGroup);

        return section;
    }

    public static HorizontalListNode CreateFilteredDropdown<T>(
        Func<List<T>> optionsProvider,
        Func<T, string> displaySelector,
        Func<T, uint>? iconSelector,
        Action<T?> setter,
        bool allowNoResult = true)
    {
        var flexNode = new HorizontalListNode
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

    public static VerticalListNode CreateStatusListNode(
        HashSet<uint> filterList,
        List<LuminaStatus> allStatuses,
        Action<uint> onRemove = null)
    {
        var statusListNode = new VerticalListNode
        {
            Width = 300,
            IsVisible = true,
            ItemSpacing = 4,
            FitContents = true,
        };

        foreach (var rowId in filterList.ToList().Order())
        {
            var status = allStatuses.FirstOrDefault(s => s.RowId == rowId);

            var row = new HorizontalListNode
            {
                X = ConfigurationUIFactory.OptionOffset,
                Height = 32,
                Width = 300,
                IsVisible = true,
                ItemSpacing = 4
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

    private static void TryExportFilterListToClipboard(HashSet<uint> filterList)
    {
        var exportString = Util.SerializeFilterList(filterList);
        ImGui.SetClipboardText(exportString);
        Notification notification = new() {
            Content = "Filter list exported to clipboard.",
            Type = NotificationType.Success,
        };
        GlobalServices.NotificationManager.AddNotification(notification);
        GlobalServices.Logger.Info("Filter list exported to clipboard.");
    }

    private static void TryImportFilterListFromClipboard(StatusTimerOverlayConfig config, FilterChangedCallback? onChanged)
    {
        if (!GlobalServices.KeyState[VirtualKey.SHIFT]) {
            return;
        }
        Notification notification = new() {
            Content = "Filter list imported from clipboard.",
            Type = NotificationType.Success,
        };
        var clipboard = ImGui.GetClipboardText();
        if (!string.IsNullOrWhiteSpace(clipboard)) {
            var imported = Util.DeserializeFilterList(clipboard);
            config.FilterList.Clear();
            foreach (var id in imported) {
                config.FilterList.Add(id);
            }

            onChanged?.Invoke();
            notification.Content = $"Imported {imported.Count} filter IDs from clipboard.";
            GlobalServices.Logger.Info($"Imported {imported.Count} filter IDs from clipboard.");
        } else {
            notification.Content = "Clipboard data was invalid or could not be imported.";
            notification.Type = NotificationType.Error;
            GlobalServices.Logger.Warning("Clipboard is empty or invalid for import.");
        }
        GlobalServices.NotificationManager.AddNotification(notification);
    }
}
