using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Classes;
using StatusTimers.Helpers;
using StatusTimers.Windows;

namespace StatusTimers;

public class Plugin : IDalamudPlugin {
    public const string CommandName = "/statustimers";
    public readonly OverlayManager OverlayManager;

    public unsafe Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services.Services>();

        BackupHelper.DoConfigBackup(pluginInterface);

        Services.Services.NativeController = new NativeController(pluginInterface);
        Services.Services.NameplateAddonController = new NameplateAddonController(pluginInterface);

        OverlayManager = new OverlayManager();

        Services.Services.PluginInterface.UiBuilder.OpenMainUi += OverlayManager.ToggleConfig;
        Services.Services.PluginInterface.UiBuilder.OpenConfigUi += OverlayManager.ToggleConfig;

        Services.Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the main window"
        });

        if (Services.Services.ClientState.IsLoggedIn) {
            Services.Services.Framework.RunOnFrameworkThread(OnLogin);
        }

        Services.Services.Framework.Update += OnFrameworkUpdate;
        Services.Services.ClientState.Login += OnLogin;
        Services.Services.ClientState.Logout += OnLogout;

        Services.Services.NameplateAddonController.OnUpdate += OnNameplateUpdate;
        //ReflectionDebugWindow.Open();
    }

    public void Dispose() {
        Services.Services.Framework.Update -= OnFrameworkUpdate;
        Services.Services.CommandManager.RemoveHandler(CommandName);

        Services.Services.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
        Services.Services.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

        OverlayManager.Dispose();
        Services.Services.NativeController.Dispose();
        Services.Services.NameplateAddonController.Dispose();
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
        Services.Services.NameplateAddonController.Enable();

        #if DEBUG
            OverlayManager.OpenConfig();
        #endif
    }

    private static void OnLogout(int type, int code) {
        Services.Services.NameplateAddonController.Disable();
    }
}
