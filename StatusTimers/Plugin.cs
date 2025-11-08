using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
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

        GlobalServices.NativeController = new NativeController(pluginInterface);
        GlobalServices.OverlayAddonController = new OverlayAddonController();

        OverlayManager = new OverlayManager();

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
        GlobalServices.ClientState.Logout += OnLogout;

        GlobalServices.OverlayAddonController.OnUpdate += OnNameplateUpdate;
        //ReflectionDebugWindow.Open();
    }

    public void Dispose() {
        GlobalServices.Framework.Update -= OnFrameworkUpdate;
        GlobalServices.CommandManager.RemoveHandler(CommandName);

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

        OverlayManager.Dispose();
        GlobalServices.NativeController.Dispose();
        GlobalServices.OverlayAddonController.Dispose();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    private unsafe void OnNameplateUpdate(AddonNamePlate* nameplate) {
        OverlayManager.OnUpdate();
    }

    private void OnCommand(string command, string args) {
        if (args is "toggleall") {
            if (OverlayManager.PlayerCombinedOverlayInstance != null) {
                OverlayManager.PlayerCombinedOverlayInstance.IsVisible =
                    !OverlayManager.PlayerCombinedOverlayInstance.IsVisible;
            }

            if (OverlayManager.EnemyMultiDoTOverlayInstance != null) {
                OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible =
                    !OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible;
            }
        }
        else {
            OverlayManager.ToggleConfig();
        }
    }

    private void OnLogin() {
        GlobalServices.OverlayAddonController.Enable();

        #if DEBUG
            OverlayManager.OpenConfig();
        #endif
    }

    private static void OnLogout(int type, int code) {
        GlobalServices.OverlayAddonController.Disable();
    }
}
