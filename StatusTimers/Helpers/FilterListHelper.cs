using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using StatusTimers.Config;
using System;
using System.Collections.Generic;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Helpers;

public static class FilterListHelper {
    public static void TryExportFilterListToClipboard(HashSet<uint> filterList)
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

    public static void TryImportFilterListFromClipboard(StatusTimerOverlayConfig config, Action? onChanged)
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
