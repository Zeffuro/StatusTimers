using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using System.IO;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class ImportExportResetNode : HorizontalListNode
{
    public ImportExportResetNode(
        OverlayManager manager,
        NodeKind kind)
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
            TextTooltip = " Import Configuration\n(hold shift to confirm)",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\download.png"),
            OnClick = () => ImportExportResetHelper.TryImportConfigFromClipboard(
                manager, kind)
        });

        AddNode(new ImGuiIconButtonNode {
            Y = 3,
            Height = 30,
            Width = 30,
            IsVisible = true,
            TextTooltip = "Export Configuration",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\upload.png"),
            OnClick = () => ImportExportResetHelper.TryExportConfigToClipboard(manager, kind)
        });

        AddNode(new HoldButtonNode {
            IsVisible = true,
            Y = 0,
            Height = 32,
            Width = 100,
            String = "Reset",
            TextNode = { TextColor = ColorHelper.GetColor(50) },
            TextTooltip = "   Reset configuration\n(hold button to confirm)",
            OnClick = () => ImportExportResetHelper.TryResetConfig(
                manager, kind)
        });
    }
}
