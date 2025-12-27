using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Helpers;
using System;
using System.IO;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusFilterButtonGroupNode : HorizontalListNode {
    private readonly Func<StatusTimerOverlayConfig> _getConfig;
    private RadioButtonGroupNode _radioButtonGroup;
    private ImGuiIconButtonNode _exportButtonNode;
    private ImGuiIconButtonNode _importButtonNode;

    public StatusFilterButtonGroupNode(Func<StatusTimerOverlayConfig> getConfig, Action onChanged) {
        _getConfig = getConfig;

        _radioButtonGroup = new RadioButtonGroupNode {
            Width = 100,
            Height = 60,
            IsVisible = true,
        };
        _radioButtonGroup.AddButton("Blacklist", () =>
        {
            getConfig().FilterIsBlacklist = true;
        });
        _radioButtonGroup.AddButton("Whitelist", () =>
        {
            getConfig().FilterIsBlacklist = false;
        });

        _radioButtonGroup.SelectedOption = getConfig().FilterIsBlacklist ? "Blacklist" : "Whitelist";
        AddNode(_radioButtonGroup);

        _exportButtonNode = new ImGuiIconButtonNode {

            Height = 30,
            Width = 30,
            IsVisible = true,
            TextTooltip = "Export Filter List",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\upload.png"),
            OnClick = () => FilterListHelper.TryExportFilterListToClipboard(getConfig().FilterList)
        };
        AddNode(_exportButtonNode);

        _importButtonNode = new ImGuiIconButtonNode {
            X = 52,
            Height = 30,
            Width = 30,
            IsVisible = true,
            TextTooltip = "     Import Filter List \n(hold shift to confirm)",
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Icons\download.png"),
            OnClick = () => FilterListHelper.TryImportFilterListFromClipboard(getConfig(), onChanged)
        };
        AddNode(_importButtonNode);

    }

}
