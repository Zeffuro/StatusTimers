using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using StatusTimers.Enums;
using StatusTimers.Windows;
using System;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Helpers;

public static class ImportExportResetHelper {
    public static void Run(Func<string> action) {
        try {
            GlobalServices.NotificationManager.AddNotification(new Notification {
                Content = action(), Type = NotificationType.Success
            });
        }
        catch (Exception ex) {
            GlobalServices.Logger.Warning(ex, "StatusTimers configuration action failed.");
            GlobalServices.NotificationManager.AddNotification(new Notification {
                Content = ex.Message, Type = NotificationType.Error
            });
        }
    }

    public static void TryImportConfigFromClipboard(OverlayManager manager, NodeKind kind) {
        if (GlobalServices.KeyState[VirtualKey.SHIFT]) {
            Run(() => manager.ImportOverlay(kind, ImGui.GetClipboardText()));
        }
    }

    public static void TryExportConfigToClipboard(OverlayManager manager, NodeKind kind) => Run(() => {
        ImGui.SetClipboardText(manager.ExportOverlay(kind));
        return "Configuration exported to clipboard.";
    });

    public static void TryResetConfig(OverlayManager manager, NodeKind kind) => Run(() => manager.ResetOverlay(kind));
}
