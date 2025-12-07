using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Overlay;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers;

public class Plugin : IDalamudPlugin {
    public const string CommandName = "/statustimers";
    public readonly OverlayManager OverlayManager;

    public unsafe Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<GlobalServices>();

        BackupHelper.DoConfigBackup(pluginInterface);

        KamiToolKitLibrary.Initialize(pluginInterface);
        GlobalServices.OverlayController = new OverlayController();

        OverlayManager = new OverlayManager();

        OverlayManager.Setup();

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi += OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi += OverlayManager.ToggleConfig;

        GlobalServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the main window"
        });

        if (GlobalServices.ClientState.IsLoggedIn) {
            GlobalServices.Framework.RunOnFrameworkThread(OnLogin);
        }

        GlobalServices.Framework.Update += OnFrameworkUpdate;
        GlobalServices.ClientState.Login += OnLogin;
    }

    public void Dispose() {
        GlobalServices.Framework.Update -= OnFrameworkUpdate;
        GlobalServices.CommandManager.RemoveHandler(CommandName);

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

        OverlayManager.Dispose();
        KamiToolKitLibrary.Dispose();
        GlobalServices.OverlayController.Dispose();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    private void OnCommand(string command, string args) {
        if (args is "toggleall") {
            OverlayManager.PlayerCombinedOverlayInstance?.IsVisible = !OverlayManager.PlayerCombinedOverlayInstance.IsVisible;
            OverlayManager.EnemyMultiDoTOverlayInstance?.IsVisible = !OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible;
        }
        else {
            OverlayManager.ToggleConfig();
        }
    }

    private void OnLogin() {
        #if DEBUG
            OverlayManager.OpenConfig();
        #endif
    }
}
