using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using StatusTimers.Helpers;
using StatusTimers.Windows;

namespace StatusTimers;

public class Plugin : IDalamudPlugin {
    public const string CommandName = "/statustimers";
    public readonly OverlayManager OverlayManager;
    public Configuration Configuration;

    public Plugin(IDalamudPluginInterface pluginInterface) {
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
        OverlayManager.OnUpdate();
    }

    private void OnCommand(string command, string args) {
        if (args is "settings" or "config")
        {
            OverlayManager.ToggleConfig();
        }
        else {
            OverlayManager.OpenAll();
        }
    }

    private static void OnLogin() {
        Services.NameplateAddonController.Enable();
    }

    private static void OnLogout(int type, int code) {
        Services.NameplateAddonController.Disable();
    }
}
