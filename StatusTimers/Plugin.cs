using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using KamiToolKit.UiOverlay;
using StatusTimers.Extensions;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using System.Threading;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers;

public class Plugin : IAsyncDalamudPlugin {
    public const string CommandName = "/statustimers";

    [PluginService]
    private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    private OverlayManager OverlayManager { get; set; } = null!;

    public async Task LoadAsync(CancellationToken cancellationToken) {
        PluginInterface.Create<GlobalServices>();

        BackupHelper.DoConfigBackup(PluginInterface);

        KamiToolKitLibrary.Initialize(PluginInterface);
        await GlobalServices.Framework.Run(() => {
            GlobalServices.OverlayController = new OverlayController();
        }, cancellationToken);

        OverlayManager = new OverlayManager();

        await OverlayManager.SetupAsync(cancellationToken);

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi += OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi += OverlayManager.ToggleConfig;

        GlobalServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the main window"
        });

        if (GlobalServices.ClientState.IsLoggedIn) {
            await GlobalServices.Framework.Run(OnLogin, cancellationToken);
        }

        GlobalServices.Framework.Update += OnFrameworkUpdate;
        GlobalServices.ClientState.Login += OnLogin;
    }

    public async ValueTask DisposeAsync() {
        GlobalServices.Framework.Update -= OnFrameworkUpdate;
        GlobalServices.ClientState.Login -= OnLogin;
        GlobalServices.CommandManager.RemoveHandler(CommandName);

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

        await OverlayManager.DisposeAsync();

        await GlobalServices.Framework.RunSafely(() => GlobalServices.OverlayController.Dispose());
        await GlobalServices.Framework.RunSafely(KamiToolKitLibrary.Dispose);
    }

    private void OnFrameworkUpdate(IFramework framework) {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    private void OnCommand(string command, string args) {
        if (args is "toggleall") {
            GlobalServices.Framework.RunOnFrameworkThread(() => {
                OverlayManager.PlayerCombinedOverlayInstance?.IsVisible = !OverlayManager.PlayerCombinedOverlayInstance.IsVisible;
                OverlayManager.EnemyMultiDoTOverlayInstance?.IsVisible = !OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible;
                OverlayManager.PlayerBuffsOverlayInstance?.IsVisible = !OverlayManager.PlayerBuffsOverlayInstance.IsVisible;
                OverlayManager.PlayerDebuffsOverlayInstance?.IsVisible = !OverlayManager.PlayerDebuffsOverlayInstance.IsVisible;
            });
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
