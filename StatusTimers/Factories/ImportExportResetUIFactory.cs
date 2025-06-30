using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using System;
using System.Linq;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public static class ImportExportResetUIFactory
{
    public static HorizontalListNode<NodeBase> Create(
        StatusTimerOverlayConfig currentOverlayConfig,
        NodeKind kind,
        Action onConfigChanged,
        Action closeWindow)
    {
        var node = new HorizontalListNode<NodeBase> {
            Height = 0,
            Width = 600,
            Alignment = HorizontalListAnchor.Left,
            FirstItemSpacing = 440,
            ItemHorizontalSpacing = 0,
            IsVisible = true,
        };

        // Export Button
        node.AddNode(new CircleButtonNode {
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = "Export Configuration",
            Icon = ButtonIcon.Document,
            AddColor = new Vector3(150, 0, 150),
            OnClick = () => {
                var exportString = Util.SerializeConfig(currentOverlayConfig);
                ImGui.SetClipboardText(exportString);
                GlobalServices.NotificationManager.AddNotification(
                  new Notification { Content = "Configuration exported to clipboard.", Type = NotificationType.Success }
                );
                GlobalServices.Logger.Info("Configuration exported to clipboard.");
            }
        });

        // Import Button
        node.AddNode(new CircleButtonNode {
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = " Import Configuration\n(hold shift to confirm)",
            Icon = ButtonIcon.Document,
            AddColor = new Vector3(0, 150, 150),
            OnClick = () => {
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
        });

        // Reset Configuration Hold Button
        node.AddNode(new HoldButtonNode {
            IsVisible = true,
            Y = -3,
            Height = 32,
            Width = 100,
            Label = "Reset",
            Tooltip = "   Reset configuration\n(hold button to confirm)",
            OnClick = () => {
                Util.ResetConfig(currentOverlayConfig, kind);
                GlobalServices.NotificationManager.AddNotification(
                  new Notification { Content = "Configuration reset to default.", Type = NotificationType.Success }
                );
                GlobalServices.Logger.Info("Configuration reset to default.");
                onConfigChanged?.Invoke();
                closeWindow?.Invoke();
            }
        });

        return node;
    }
}
