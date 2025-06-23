using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using StatusTimers.Helpers;
using StatusTimers.Windows;

namespace StatusTimers;

public class Plugin : IDalamudPlugin {
    public const string CommandName = "/statustimers";
    public readonly OverlayManager OverlayManager;
    public Configuration Configuration;

    public unsafe Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();

        Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Services.NativeController = new NativeController(pluginInterface);
        Services.NameplateAddonController = new NameplateAddonController(pluginInterface);

        OverlayManager = new OverlayManager();


        Services.PluginInterface.UiBuilder.OpenMainUi += OverlayManager.ToggleConfig;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OverlayManager.ToggleConfig;

        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the main window"
        });

        if (Services.ClientState.IsLoggedIn) {
            Services.Framework.RunOnFrameworkThread(OnLogin);
        }

        Services.Framework.Update += OnFrameworkUpdate;
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;

        Services.NameplateAddonController.OnUpdate += OnNameplateUpdate;

        OverlayManager.OpenAll();
        OverlayManager.ToggleConfig();
    }

    public void Dispose() {
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.CommandManager.RemoveHandler(CommandName);

        Configuration.Save();

        Services.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

        OverlayManager.Dispose();
        Services.NativeController.Dispose();
        Services.NameplateAddonController.Dispose();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    private unsafe void OnNameplateUpdate(AddonNamePlate* nameplate) {
        OverlayManager.OnUpdate();
    }

    private void OnCommand(string command, string args) {

        if (args is "toggleall") {
            OverlayManager.PlayerCombinedOverlayInstance.IsVisible =
                !OverlayManager.PlayerCombinedOverlayInstance.IsVisible;
            OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible =
                !OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible;
        }
        else {
            OverlayManager.ToggleConfig();
        }
    }

    private static void OnLogin() {
        Services.NameplateAddonController.Enable();
    }

    private static void OnLogout(int type, int code) {
        Services.NameplateAddonController.Disable();
    }
}
