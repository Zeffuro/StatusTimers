using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
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
using StatusTimers.Nodes;
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
        Action? onChanged = null,
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

        section.AddDummy(ConfigurationUIFactory.CheckBoxHeight);

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

        var statusListNode = new StatusFilterListNode (allStatuses, getConfig().FilterList, onChanged) {
            Width = 300,
            IsVisible = true,
            ItemSpacing = 4,
            FitContents = true,
        };

        var filteredDropdownNode = new StatusFilterDropdownNode<LuminaStatus>(
            () => allStatuses,
            status => $"{status.RowId} {status.Name.ExtractText()}",
            status => status.Icon,
            getConfig().FilterList,
            statusListNode,
            onChanged: () => statusListNode.Refresh()
        );

        filterContentGroup.AddNode(filteredDropdownNode);

        filterContentGroup.AddNode(statusListNode);
        section.AddNode(filterContentGroup);

        return section;
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

    private static void TryImportFilterListFromClipboard(StatusTimerOverlayConfig config, Action? onChanged)
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
