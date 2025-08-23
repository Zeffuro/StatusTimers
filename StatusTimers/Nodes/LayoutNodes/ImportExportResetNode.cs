using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using System;
using System.IO;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class ImportExportResetNode : HorizontalListNode
{
    public ImportExportResetNode(
        Func<StatusTimerOverlay<StatusKey>> getOverlay,
        Func<StatusTimerOverlayConfig> getConfig,
        NodeKind kind,
        Action onConfigChanged,
        Action closeWindow)
    {
        Height = 0;
        Width = 600;
        Alignment = HorizontalListAnchor.Right;
        FirstItemSpacing = 3;
        ItemSpacing = 2;
        IsVisible = true;

        AddNode(new ImGuiIconButtonNode {
            Y = 3,
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = " Import Configuration\n(hold shift to confirm)",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\download.png"),
            OnClick = () => ImportExportResetHelper.TryImportConfigFromClipboard(
                getOverlay(), getConfig(), onConfigChanged, closeWindow)
        });

        AddNode(new ImGuiIconButtonNode {
            Y = 3,
            Height = 30,
            Width = 30,
            IsVisible = true,
            Tooltip = "Export Configuration",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\upload.png"),
            OnClick = () => ImportExportResetHelper.TryExportConfigToClipboard(getConfig())
        });

        AddNode(new HoldButtonNode {
            IsVisible = true,
            Y = 0,
            Height = 32,
            Width = 100,
            Label = "Reset",
            Tooltip = "   Reset configuration\n(hold button to confirm)",
            OnClick = () => ImportExportResetHelper.TryResetConfig(
                getConfig(), kind, onConfigChanged, closeWindow)
        });
    }
}
