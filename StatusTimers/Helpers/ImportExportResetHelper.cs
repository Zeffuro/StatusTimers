using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Component.GUI;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Windows;
using System;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Helpers;

public class ImportExportResetHelper {
    public static unsafe void TryImportConfigFromClipboard(
        StatusTimerOverlay<StatusKey> overlay,
        StatusTimerOverlayConfig currentOverlayConfig,
        Action onConfigChanged,
        Action closeWindow)
    {
        if (!Services.Services.KeyState[VirtualKey.SHIFT]) {
            return;
        }

        var clipboard = ImGui.GetClipboardText();
        var notification = new Notification { Content = "Configuration imported from clipboard.", Type = NotificationType.Success };

        if (!string.IsNullOrWhiteSpace(clipboard)) {
            var imported = Util.DeserializeConfig(clipboard);
            if (imported != null) {
                foreach (var prop in typeof(StatusTimerOverlayConfig).GetProperties().Where(p => p.CanRead && p.CanWrite)) {
                    prop.SetValue(currentOverlayConfig, prop.GetValue(imported));
                }

                var overlaySize = OverlayLayoutHelper.CalculateOverlaySize(imported);
                var screenSize = new Vector2(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);
                var clampedPosition = ImportExportResetHelper.EnsurePositionOnScreen(imported.Position, overlaySize, screenSize);
                currentOverlayConfig.Position = clampedPosition;

                overlay.IsVisible = imported.Enabled;
                overlay.Position = clampedPosition;
                StatusTimerOverlayConfigHelper.MigrateLegacyConfig(currentOverlayConfig);
                Services.Services.Logger.Info("Configuration imported from clipboard.");
                currentOverlayConfig.Notify("Config", updateNodes: true);
                overlay.OnUpdate();
                onConfigChanged();
                closeWindow.Invoke();

                // Restart overlay
                overlay.RestartOverlay();
            } else {
                notification.Content = "Clipboard data was invalid or could not be imported.";
                notification.Type = NotificationType.Error;
                Services.Services.Logger.Warning("Clipboard data was invalid or could not be imported.");
            }
        } else {
            notification.Content = "Clipboard is empty or invalid for import.";
            notification.Type = NotificationType.Warning;
            Services.Services.Logger.Warning("Clipboard is empty or invalid for import.");
        }
        Services.Services.NotificationManager.AddNotification(notification);
    }

    public static void TryExportConfigToClipboard(
        StatusTimerOverlayConfig currentOverlayConfig)
    {
        var exportString = Util.SerializeConfig(currentOverlayConfig);
        ImGui.SetClipboardText(exportString);
        Services.Services.NotificationManager.AddNotification(
            new Notification { Content = "Configuration exported to clipboard.", Type = NotificationType.Success }
        );
        Services.Services.Logger.Info("Configuration exported to clipboard.");
    }

    public static void TryResetConfig(
        StatusTimerOverlayConfig currentOverlayConfig,
        NodeKind kind,
        Action onConfigChanged,
        Action closeWindow)
    {
        Util.ResetConfig(currentOverlayConfig, kind);
        Services.Services.NotificationManager.AddNotification(
            new Notification { Content = "Configuration reset to default.", Type = NotificationType.Success }
        );
        Services.Services.Logger.Info("Configuration reset to default.");
        onConfigChanged.Invoke();
        closeWindow.Invoke();
    }

    public static Vector2 EnsurePositionOnScreen(Vector2 position, Vector2 overlaySize, Vector2 screenSize)
    {
        float x = Math.Clamp(position.X, 0, screenSize.X - overlaySize.X);
        float y = Math.Clamp(position.Y, 0, screenSize.Y - overlaySize.Y);
        return new Vector2(x, y);
    }
}
