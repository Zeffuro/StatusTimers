using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public static class ImportExportResetUIFactory
{
    public static HorizontalListNode<NodeBase> Create(
        Func<StatusTimerOverlayConfig> getConfig,
        NodeKind kind,
        Action onConfigChanged,
        Action closeWindow)
    {
        var node = new HorizontalListNode<NodeBase> {
            Height = 0,
            Width = 600,
            Alignment = HorizontalListAnchor.Right,
            FirstItemSpacing = 3,
            ItemHorizontalSpacing = 2,
            IsVisible = true,
        };

        // Import Button
        node.AddNode(new ImGuiIconButtonNode {
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = " Import Configuration\n(hold shift to confirm)",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\download.png"),
            OnClick = () => TryImportConfigFromClipboard(getConfig(), onConfigChanged, closeWindow)
        });

        // Export Button
        node.AddNode(new ImGuiIconButtonNode {
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = "Export Configuration",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\upload.png"),
            OnClick = () => TryExportConfigToClipboard(getConfig())
        });

        // Reset Configuration Hold Button
        node.AddNode(new HoldButtonNode {
            IsVisible = true,
            Y = -3,
            Height = 32,
            Width = 100,
            Label = "Reset",
            Tooltip = "   Reset configuration\n(hold button to confirm)",
            OnClick = () => TryResetConfig(getConfig(), kind, onConfigChanged, closeWindow)
        });

        return node;
    }

    private static void TryImportConfigFromClipboard(
        StatusTimerOverlayConfig currentOverlayConfig,
        Action onConfigChanged,
        Action closeWindow)
    {
        if (!GlobalServices.KeyState[VirtualKey.SHIFT]) {
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
                GlobalServices.Logger.Info("Configuration imported from clipboard.");
                currentOverlayConfig.Notify("Config", needsRebuild: true, updateNodes: true);
                onConfigChanged?.Invoke();
                closeWindow?.Invoke();
            } else {
                notification.Content = "Clipboard data was invalid or could not be imported.";
                notification.Type = NotificationType.Error;
                GlobalServices.Logger.Warning("Clipboard data was invalid or could not be imported.");
            }
        } else {
            notification.Content = "Clipboard is empty or invalid for import.";
            notification.Type = NotificationType.Warning;
            GlobalServices.Logger.Warning("Clipboard is empty or invalid for import.");
        }
        GlobalServices.NotificationManager.AddNotification(notification);
    }

    private static void TryExportConfigToClipboard(
        StatusTimerOverlayConfig currentOverlayConfig)
    {
        var exportString = Util.SerializeConfig(currentOverlayConfig);
        ImGui.SetClipboardText(exportString);
        GlobalServices.NotificationManager.AddNotification(
            new Notification { Content = "Configuration exported to clipboard.", Type = NotificationType.Success }
        );
        GlobalServices.Logger.Info("Configuration exported to clipboard.");
    }

    private static void TryResetConfig(
        StatusTimerOverlayConfig currentOverlayConfig,
        NodeKind kind,
        Action onConfigChanged,
        Action closeWindow)
    {
        Util.ResetConfig(currentOverlayConfig, kind);
        GlobalServices.NotificationManager.AddNotification(
            new Notification { Content = "Configuration reset to default.", Type = NotificationType.Success }
        );
        GlobalServices.Logger.Info("Configuration reset to default.");
        onConfigChanged?.Invoke();
        closeWindow?.Invoke();
    }
}
